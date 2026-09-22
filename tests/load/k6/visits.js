// Load test for the Visits API: a realistic request mix at a fixed arrival rate (300 req/s target).
//
//   BASE_URL   API base address                       default http://localhost:5072
//   TOKEN      bearer token (required); its `terminal` claim must cover TERMINALS
//   TERMINALS  space-separated UN/LOCODEs to spread the load over   default "DKCPH SEGOT"
//   PROFILE    "full" (default: ramp 30 s → 300 req/s for 120 s → 450 req/s for 30 s) or "ci" (300 req/s for 60 s)
//   P95_MS / P99_MS   latency thresholds in milliseconds    default 200 / 500
//
// Every iteration is exactly one HTTP request, so the configured arrival rate is the request rate.
// Mix: 60 % search (varied filters), 30 % get-by-id, 6 % create, 4 % status update. Status updates are made on
// visits the same VU created earlier and only ever move forward, so they never produce a 409.

import http from 'k6/http';
import { check, fail } from 'k6';
import { randomItem, randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5072';
const TOKEN = __ENV.TOKEN;
const TERMINALS = (__ENV.TERMINALS || 'DKCPH SEGOT').split(/\s+/).filter(Boolean);
const PROFILE = __ENV.PROFILE || 'full';
const P95_MS = Number(__ENV.P95_MS || 200);
const P99_MS = Number(__ENV.P99_MS || 500);

const SEED_VISITS = 200;

const stages = PROFILE === 'ci'
    ? [{ target: 300, duration: '10s' }, { target: 300, duration: '60s' }]
    : [{ target: 300, duration: '30s' }, { target: 300, duration: '120s' }, { target: 450, duration: '30s' }, { target: 450, duration: '30s' }];

export const options = {
    scenarios: {
        mixed_traffic: {
            executor: 'ramping-arrival-rate',
            startRate: 50,
            timeUnit: '1s',
            preAllocatedVUs: 50,
            maxVUs: 400,
            stages,
        },
    },
    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: [`p(95)<${P95_MS}`, `p(99)<${P99_MS}`],
        'http_req_duration{name:search}': [`p(95)<${P95_MS}`],
        'http_req_duration{name:get}': [`p(95)<${P95_MS}`],
        'http_req_duration{name:create}': [`p(95)<${P95_MS}`],
        'http_req_duration{name:status}': [`p(95)<${P95_MS}`],
        checks: ['rate>0.99'],
        dropped_iterations: ['count<100'], // the runner could not keep the arrival rate (VU starvation), not an API failure
    },
    summaryTrendStats: ['avg', 'med', 'p(90)', 'p(95)', 'p(99)', 'max'],
};

const headers = {
    Authorization: `Bearer ${TOKEN}`,
    'Content-Type': 'application/json',
};

const LOCATIONS = ['SEGOT', 'SESTO', 'NLRTM', 'DEHAM', 'TRIST', 'PLGDN'];

// A movement's location must differ from the visited terminal (domain rule), so the terminal is excluded.
function externalLocation(terminal) {
    return randomItem(LOCATIONS.filter((l) => l !== terminal));
}

function createBody(terminal, n) {
    return JSON.stringify({
        terminalId: terminal,
        truck: { unitNumber: `tr ${n}`, licensePlate: `ab ${n % 1000} ${n}`, carrier: 'Load Test Haulage' },
        driver: { name: `Driver ${n}`, licenseNumber: `dl ${n}`, phone: '+4512345678' },
        movements: [
            { type: 'Delivery', unitNumber: `cont ${n}a`, location: externalLocation(terminal), reference: `BK-${n}` },
            { type: 'Collection', unitNumber: `cont ${n}b`, location: externalLocation(terminal) },
        ],
    });
}

export function setup() {
    if (!TOKEN) {
        fail('TOKEN is required (dotnet user-jwts create --claim "terminal=DKCPH SEGOT" --output token)');
    }

    const ready = http.get(`${BASE_URL}/health/ready`);
    if (ready.status !== 200) {
        fail(`API is not ready at ${BASE_URL}: ${ready.status} ${ready.body}`);
    }

    const ids = [];
    for (let i = 0; i < SEED_VISITS; i++) {
        const res = http.post(`${BASE_URL}/api/visits`, createBody(TERMINALS[i % TERMINALS.length], i), { headers, tags: { name: 'seed' } });
        if (res.status !== 201) {
            fail(`seeding failed: ${res.status} ${res.body}`);
        }
        ids.push(res.json('id'));
    }

    return { ids };
}

// Per-VU queue of visits this VU created, with the next status each one can take.
const pending = [];
let created = 0;

const NEXT = { PreRegistered: 'AtGate', AtGate: 'OnSite', OnSite: 'Completed' };

function searchQuery() {
    const terminal = randomItem(TERMINALS);
    return randomItem([
        '',
        `terminalId=${terminal}`,
        `terminalId=${terminal}&currentStatus=AtGate`,
        `terminalId=${terminal}&movementFrom=SE`,
        `terminalId=${terminal}&movementTo=NLRTM`,
        `terminalId=${terminal}&movementFrom=SEGOT&movementTo=${terminal}`,
        `terminalId=${terminal}&createdBy=gate-operator`,
        `terminalId=${terminal}&page=2&pageSize=50`,
        `currentStatus=PreRegistered&pageSize=100`,
    ]);
}

function doSearch() {
    const res = http.get(`${BASE_URL}/api/visits?${searchQuery()}`, { headers, tags: { name: 'search' } });
    check(res, { 'search 200': (r) => r.status === 200 });
}

function doGet(data) {
    const id = pending.length > 0 && Math.random() < 0.3 ? randomItem(pending).id : randomItem(data.ids);
    const res = http.get(`${BASE_URL}/api/visits/${id}`, { headers, tags: { name: 'get' } });
    check(res, { 'get 200': (r) => r.status === 200 });
}

function doCreate() {
    created += 1;
    const n = __VU * 100000 + created;
    const res = http.post(`${BASE_URL}/api/visits`, createBody(randomItem(TERMINALS), n), { headers, tags: { name: 'create' } });
    if (check(res, { 'create 201': (r) => r.status === 201 })) {
        pending.push({ id: res.json('id'), status: 'PreRegistered' });
    }
}

function doStatus() {
    if (pending.length === 0) {
        doCreate();
        return;
    }

    const index = randomIntBetween(0, pending.length - 1);
    const visit = pending[index];
    const target = NEXT[visit.status];
    const res = http.post(`${BASE_URL}/api/visits/${visit.id}/status`, JSON.stringify({ status: target, reason: 'load test' }), { headers, tags: { name: 'status' } });

    if (check(res, { 'status 200': (r) => r.status === 200 })) {
        visit.status = target;
        if (target === 'Completed') {
            pending.splice(index, 1);
        }
    }
}

export default function (data) {
    const roll = Math.random();
    if (roll < 0.60) {
        doSearch();
    } else if (roll < 0.90) {
        doGet(data);
    } else if (roll < 0.96) {
        doCreate();
    } else {
        doStatus();
    }
}
