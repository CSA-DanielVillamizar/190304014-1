import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 50 },  // Rampa de subida
    { duration: '1m', target: 100 }, // Meseta de alta carga
    { duration: '30s', target: 0 },   // Rampa de bajada
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],   // Error rate menor al 1%
    http_req_duration: ['p(95)<500'], // 95% de peticiones < 500ms
  },
};

export default function () {
  const url = 'http://api.itm-tickets.com/orders'; // Ingress de la Clase 16
  const payload = JSON.stringify({ productId: 1, quantity: 1 });
  const params = { headers: { 'Content-Type': 'application/json' } };

  const res = http.post(url, payload, params);

  check(res, {
    'status is 201': (r) => r.status === 201,
  });

  sleep(1); // "Think time" del usuario real
}