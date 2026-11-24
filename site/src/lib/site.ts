import { serviceGroups } from './services';

export const site = {
  name: 'Chris Woody Woodruff',
  shortName: 'Woody',
  initials: 'CW',
  title: 'Chris Woody Woodruff | Fractional Architect, Strategic Advisor & Expert Witness',
  description:
    'Chris Woody Woodruff helps teams untangle complexity in software systems: fractional architecture, agentic developer relations, expert witness work, and strategic technology advice.',
  roles: ['Fractional Architect', 'Strategic Advisor', 'Expert Witness'],
  email: 'cwoodruff@live.com',
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
    substack: 'https://simplicityfirstphilosophy.substack.com/',
  },
  credentials: [
    'Microsoft MVP in .NET and Web Development, first awarded 2008, re-awarded 2025',
    'Board director, .NET Foundation',
    'JetBrains Community Contributor',
    '50+ conference talks across North America and Europe',
    'Co-host, The Breakpoint Show',
    'Author of the Simplicity-First Review newsletter',
    'Coined the term Agentic Developer Relations',
    'Founder, EcoSynt',
  ],
} as const;

export const services = [
  { slug: 'fractional-architect', name: 'Fractional Architect', group: 'ongoing' },
  { slug: 'advisory', name: 'Advisory & Board Roles', group: 'ongoing' },
  { slug: 'micro-consulting', name: 'Micro-Consulting', group: 'defined' },
  { slug: 'project-based', name: 'Project-Based Contracts', group: 'defined' },
  { slug: 'expert-witness', name: 'Expert Witness', group: 'specialist' },
  { slug: 'agentic-developer-relations', name: 'Agentic Developer Relations', group: 'specialist' },
] as const;

export const nav = [
  {
    label: 'Services',
    path: '/services/',
    groups: serviceGroups.map((g) => ({
      label: g.label,
      children: services.filter((s) => s.group === g.key).map((s) => ({ label: s.name, path: `/services/${s.slug}/` })),
    })),
  },
  { label: 'Training', path: '/training/' },
  { label: 'Portfolio', path: '/portfolio/' },
  { label: 'Blog', path: '/blog/' },
  { label: 'Press', path: '/press-media/' },
  { label: 'About', path: '/about/' },
];
