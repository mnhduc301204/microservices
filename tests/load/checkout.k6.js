import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Rate } from "k6/metrics";

export const options = {
  scenarios: {
    checkout: {
      executor: "ramping-vus",
      stages: [
        { duration: "30s", target: 20 },
        { duration: "2m", target: 50 },
        { duration: "30s", target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.05"],
    http_req_duration: ["p(95)<1000"],
    checkout_latency: ["p(95)<1500"],
    checkout_failed: ["rate<0.05"],
  },
};

const baseUrl = __ENV.GATEWAY_URL || "https://localhost:7000";
const sku = __ENV.SKU || "SKU-001";
const productName = __ENV.PRODUCT_NAME || "Load Test Product";
const unitPrice = Number(__ENV.UNIT_PRICE || "10");
const checkoutLatency = new Trend("checkout_latency");
const checkoutFailed = new Rate("checkout_failed");

function uuid() {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = Math.random() * 16 | 0;
    const v = c === "x" ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
}

function token() {
  const response = http.post(
    `${baseUrl}/auth/dev-token`,
    JSON.stringify({ subject: `load-user-${__VU}`, role: "Customer" }),
    { headers: { "Content-Type": "application/json" } },
  );

  check(response, { "token issued": (r) => r.status === 200 });
  return response.json("accessToken");
}

export default function () {
  const customerId = uuid();
  const headers = {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token()}`,
  };

  const addItem = http.post(
    `${baseUrl}/api/basket/items`,
    JSON.stringify({
      customerId,
      sku,
      productName,
      unitPrice,
      quantity: 1,
    }),
    { headers },
  );

  check(addItem, { "item added": (r) => r.status >= 200 && r.status < 300 });

  const started = Date.now();
  const checkout = http.post(
    `${baseUrl}/api/basket/${customerId}/checkout`,
    JSON.stringify({
      customerId,
      customerEmail: `${customerId}@load.test`,
    }),
    { headers },
  );

  checkoutLatency.add(Date.now() - started);
  const ok = checkout.status >= 200 && checkout.status < 300;
  checkoutFailed.add(!ok);
  check(checkout, { "checkout accepted": () => ok });

  sleep(1);
}
