# Inara — History

## Core Context

- **Role:** Content & SEO Specialist
- **Joined:** 2026-11-25

## Learnings

<!-- Append new learnings below -->

- 2026-11-25: GA4 (G-ZGNC3D2CKX) added to site/src/layouts/Base.astro, gated on import.meta.env.PROD. Learning: define:vars wraps the inline script in an IIFE, so gtag() is not global (window.dataLayer is). Any later event tracking needs `window.gtag = gtag`.
