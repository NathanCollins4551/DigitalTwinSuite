const express = require('express');
const path = require('path');

/**
 * Middleware for serving Unity WebGL files with proper headers
 */
const unityStatic = express.static(path.join(__dirname, '../../public/unity'), {
  setHeaders: (res, filePath) => {
    // Re-assert security headers for assets
    res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
    res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
    res.setHeader('Cross-Origin-Resource-Policy', 'cross-origin');
    
    // Handle Unity's Brotli compressed files
    if (filePath.toLowerCase().endsWith('.unityweb')) {
      res.setHeader('Content-Encoding', 'br');
      
      // Map specific unityweb extensions to correct types
      if (filePath.toLowerCase().endsWith('.wasm.unityweb')) {
        res.setHeader('Content-Type', 'application/wasm');
      } else if (filePath.toLowerCase().endsWith('.framework.js.unityweb')) {
        res.setHeader('Content-Type', 'application/javascript');
      } else if (filePath.toLowerCase().endsWith('.data.unityweb')) {
        res.setHeader('Content-Type', 'application/octet-stream');
      }
    }
  }
});

/**
 * Combined middleware for Unity assets
 */
const handleUnityAssets = (req, res, next) => {
  unityStatic(req, res, next);
};

module.exports = { handleUnityAssets };
