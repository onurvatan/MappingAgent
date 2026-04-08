type PageNavProps = {
  activePage: 'queue' | 'operations'
  onSelectPage: (page: 'queue' | 'operations') => void
}

export function PageNav({ activePage, onSelectPage }: PageNavProps) {
  return (
    <nav className="page-nav">
      <button
        type="button"
        className={`page-nav-button ${activePage === 'queue' ? 'page-nav-button-active' : ''}`}
        onClick={() => onSelectPage('queue')}
      >
        Queue
      </button>
      <button
        type="button"
        className={`page-nav-button ${activePage === 'operations' ? 'page-nav-button-active' : ''}`}
        onClick={() => onSelectPage('operations')}
      >
        Operations
      </button>
    </nav>
  )
}
