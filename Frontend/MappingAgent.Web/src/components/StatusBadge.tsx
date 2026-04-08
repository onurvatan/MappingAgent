import type { Bootstrap } from '../types'
import { statusLabel } from '../utils'

type StatusBadgeProps = {
  status: number
  bootstrap: Bootstrap | null
}

const statusIconFallback = ['○', '◔', '◔', '◔', '◔', '◔', '△', '●', '✕']

export function StatusBadge({ status, bootstrap }: StatusBadgeProps) {
  const icon = statusIconFallback[status] ?? '○'

  return (
    <span className={`status-badge status-badge-${status}`}>
      <span className="status-badge-icon" aria-hidden="true">
        {icon}
      </span>
      <span>{statusLabel(status, bootstrap)}</span>
    </span>
  )
}
