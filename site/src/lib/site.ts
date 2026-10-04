export const site = {
  name: 'Chris Woody Woodruff',
  shortName: 'Woody',
  initials: 'CW',
  title: 'Chris Woody Woodruff | Fractional Architect, Strategic Advisor & Expert Witness',
  description:
    'Chris Woody Woodruff helps teams untangle complexity in software systems: fractional architecture, agentic developer relations, expert witness work, and strategic technology advice.',
  roles: ['Fractional Architect', 'Strategic Advisor', 'Expert Witness'],
  email: 'chris@woodruff.dev',
  phone: '+1 616.724.6885',
  phoneHref: 'tel:+16167246885',
  location: 'Wyoming, MI 49418',
  resume: '/Christopher_Woodruff_Executive_Resume.pdf',
  /** Web3Forms public access key. Paste the key from web3forms.com here. */
  web3formsKey: 'a512aa6c-77ab-4a5c-a245-24faae8a6c87',
  social: {
    linkedin: 'https://www.linkedin.com/in/chriswoodruff/',
    github: 'https://github.com/cwoodruff',
    youtube: 'https://www.youtube.com/@ChrisWoodruff',
    bluesky: 'https://bsky.app/profile/woodruff.dev',
    mastodon: 'https://mastodon.social/@cwoodruff',
    podcast: 'https://thebreakpoint.show/',
  },
  network: [
    {
      name: 'Simplicity-First',
      url: 'https://simplicity-first.dev/',
      blurb:
        'Essays, patterns, and case studies on choosing boring technology, resisting complexity, and designing systems that stay readable at year five.',
    },
    {
      name: 'Agentic Developer Relations',
      url: 'https://agenticairelations.com/',
      blurb:
        'The discipline that makes sure AI coding agents can integrate with, consume, and represent your platform accurately. Frameworks, patterns, roles, and measurement.',
    },
  ],
} as const;

export const services = [
  { slug: 'fractional-architect', name: 'Fractional Architect' },
  { slug: 'expert-witness', name: 'Expert Witness' },
  { slug: 'micro-consulting', name: 'Micro-Consulting' },
  { slug: 'project-based', name: 'Project-Based Contracts' },
  { slug: 'agentic-developer-relations', name: 'Agentic Developer Relations' },
  { slug: 'advisory', name: 'Advisory & Board Roles' },
] as const;

export const nav = [
  { label: 'Home', path: '/' },
  {
    label: 'Services',
    path: '/services/',
    children: services.map((s) => ({ label: s.name, path: `/services/${s.slug}/` })),
  },
  { label: 'Portfolio', path: '/portfolio/' },
  { label: 'Press & Media', path: '/press-media/' },
  { label: 'Blog & Insights', path: '/blog/' },
  { label: 'Training', path: '/training/' },
  {
    label: 'Network',
    children: site.network.map((n) => ({ label: n.name, path: n.url, external: true })),
  },
  { label: 'About', path: '/about/' },
] as const;
