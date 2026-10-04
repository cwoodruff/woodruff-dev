import rss from '@astrojs/rss';
import type { APIContext } from 'astro';
import { excerpt, getPosts } from '../lib/posts';
import { site } from '../lib/site';
import { href } from '../lib/url';

export async function GET(context: APIContext) {
  const posts = (await getPosts()).slice(0, 50);
  return rss({
    title: `${site.name}: Blog & Insights`,
    description: site.description,
    site: context.site!,
    items: posts.map((post) => ({
      title: post.data.title,
      pubDate: post.data.date,
      description: excerpt(post, 300),
      link: href(`/blog/${post.id}/`),
      categories: [...post.data.categories, ...post.data.tags],
    })),
    customData: '<language>en-us</language>',
  });
}
