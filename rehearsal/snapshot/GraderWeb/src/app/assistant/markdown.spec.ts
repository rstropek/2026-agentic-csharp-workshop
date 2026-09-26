import { renderMarkdown } from './markdown';

describe('renderMarkdown', () => {
  function render(markdown: string) {
    const container = document.createElement('div');
    container.innerHTML = renderMarkdown(markdown);
    return container;
  }

  it('renders the supported subset', () => {
    const html = render(
      'Four **students**, *italic*, `code` and [a link](https://example.com).\n\n- A\n- B\n\n1. One\n2. Two',
    );

    expect(html.querySelector('p strong')?.textContent).toBe('students');
    expect(html.querySelector('p em')?.textContent).toBe('italic');
    expect(html.querySelector('p code')?.textContent).toBe('code');
    const link = html.querySelector('a') as HTMLAnchorElement;
    expect(link.href).toBe('https://example.com/');
    expect(link.target).toBe('_blank');
    expect(Array.from(html.querySelectorAll('ul > li')).map((li) => li.textContent)).toEqual([
      'A',
      'B',
    ]);
    expect(html.querySelectorAll('ol > li')).toHaveLength(2);
  });

  it('renders unsupported elements as text and flattens nested lists', () => {
    const html = render(
      '## Heading\n\n```\nblock\n```\n\n<b onclick="x()">raw</b>\n\n![alt text](x.png)\n\n- Outer\n  - Inner',
    );

    expect(html.querySelector('h1, h2, h3, pre, img, b')).toBeNull();
    expect(html.querySelector('p')?.textContent).toBe('Heading');
    expect(html.textContent).toContain('<b onclick="x()">raw</b>');
    expect(html.textContent).toContain('alt text');
    expect(html.querySelectorAll('ul')).toHaveLength(1);
    expect(Array.from(html.querySelectorAll('li')).map((li) => li.textContent)).toEqual([
      'Outer',
      'Inner',
    ]);
  });

  it('right-aligns number columns of tables', () => {
    const html = render(
      '| Student | Percentage |\n| --- | --- |\n| Walsh, Finn | 58 % |\n| Baker, Liam | ~67.5 % |\n| Reed, Caleb | – |',
    );

    expect(html.querySelector('.table-scroll > table')).not.toBeNull();
    const numeric = Array.from(html.querySelectorAll('th, td')).map((cell) =>
      cell.classList.contains('num'),
    );
    expect(numeric).toEqual([false, true, false, true, false, true, false, true]);
  });
});
