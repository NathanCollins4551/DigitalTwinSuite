const express = require('express');
const path = require('path');
const router = express.Router();

// Helper to serve HTML files
const servePage = (pageName) => (req, res) => {
  res.sendFile(path.join(__dirname, `../../public/${pageName}.html`));
};

// Main Entry Point (Dashboard)
router.get(['/', '/dashboard'], servePage('dashboard'));

// Unity Routes
router.get(['/unity', '/unity/index.html'], (req, res) => {
  res.sendFile(path.join(__dirname, '../../public/unity/index.html'));
});

module.exports = router;
