const CANONICAL_HOST = 'cdn.queenzone.org';

export default {
  async fetch(request) {
    if (request.method !== 'GET' && request.method !== 'HEAD') {
      return new Response('Method Not Allowed', {
        status: 405,
        headers: { Allow: 'GET, HEAD' }
      });
    }

    const incomingUrl = new URL(request.url);
    if (incomingUrl.pathname === '/robots.txt') {
      return new Response('User-agent: *\nDisallow: /\n', {
        headers: {
          'Cache-Control': 'public, max-age=86400',
          'Content-Type': 'text/plain; charset=utf-8',
          'X-Content-Type-Options': 'nosniff'
        }
      });
    }

    incomingUrl.protocol = 'https:';
    incomingUrl.hostname = CANONICAL_HOST;
    incomingUrl.port = '';
    return Response.redirect(incomingUrl.toString(), 301);
  }
};
