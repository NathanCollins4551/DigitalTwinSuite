const express = require('express');
const http = require('http');
const router = express.Router();

/**
 * Video Stream Proxy
 * Pipes the external video feed to the frontend to avoid CORS/Mixed content issues
 */
router.get('/video', (req, res) => {
  const target = process.env.CV_PERSONNEL_URL || 'http://cv-personnel:9002/video_feed';
  const proxyReq = http.request(target, (proxyRes) => {
    res.writeHead(proxyRes.statusCode, proxyRes.headers);
    proxyRes.pipe(res);
  });
  
  proxyReq.on('error', (e) => {
    console.error('Video proxy error:', e);
    res.status(500).end();
  });
  
  proxyReq.end();
});

/**
 * Live Tracking Data Proxy
 */
router.get('/tracking-data', (req, res) => {
  const target = process.env.CV_PERSONNEL_DATA_URL || 'http://cv-personnel:9002/api/tracking/live';
  
  const proxyReq = http.request(target, (proxyRes) => {
    let data = '';
    proxyRes.on('data', (chunk) => { data += chunk; });
    proxyRes.on('end', () => {
      res.writeHead(proxyRes.statusCode, proxyRes.headers);
      res.end(data);
    });
  });

  proxyReq.on('error', (e) => {
    console.error('Tracking data proxy error:', e);
    res.status(500).end();
  });

  proxyReq.end();
});

module.exports = router;
