import { services } from './site';

/**
 * Contact form subjects. Link to the form with a subject preselected via
 * contactPath('<key>'), e.g. /contact/?subject=expert-witness.
 */
export const contactSubjects = [
  ...services.map((s) => ({ key: s.slug as string, label: s.name as string })),
  { key: 'training', label: 'Training or a private workshop' },
  { key: 'press', label: 'Press, podcast, or speaking' },
  { key: 'other', label: 'Something else' },
];

export const contactPath = (subject: string) => `/contact/?subject=${subject}`;
