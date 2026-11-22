/**
 * Service groups, keyed by the `group` field in the services collection.
 * Order here is the order the groups appear on the services page.
 */
export const serviceGroups = [
  {
    key: 'ongoing',
    label: 'Ongoing leadership',
    situation: 'We need senior technical judgment every week, not just once.',
    text: 'For teams and boards that need an experienced architect in the room, week after week, without a full-time hire.',
  },
  {
    key: 'defined',
    label: 'A defined problem',
    situation: 'We have one problem, and we need it solved.',
    text: 'For a single sharp question or a scoped piece of work with a deadline: answered or delivered, in writing, then handed back.',
  },
  {
    key: 'specialist',
    label: 'Specialist work',
    situation: 'We need a specialist for litigation or AI-agent readiness.',
    text: 'Forensic software analysis for attorneys, and developer relations for platforms whose newest users are AI coding agents.',
  },
] as const;

export type ServiceGroupKey = (typeof serviceGroups)[number]['key'];

export const groupLabel = (key: ServiceGroupKey) => serviceGroups.find((g) => g.key === key)!.label;
