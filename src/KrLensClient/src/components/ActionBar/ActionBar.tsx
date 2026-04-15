import type { ChangeEvent } from 'react'
import styles from './ActionBar.module.css'
import { DOWNLOAD_FORMATS, type DownloadFormat } from '../../types'

interface ActionBarProps {
  canUndo: boolean
  canRedo: boolean
  canRevert: boolean
  canRotate: boolean
  canDownload: boolean
  isBusy: boolean
  downloadFormat: DownloadFormat
  onUndo: () => Promise<void> | void
  onRedo: () => Promise<void> | void
  onRevert: () => Promise<void> | void
  onRotateRight: () => Promise<void> | void
  onDownload: () => Promise<void> | void
  onDownloadFormatChange: (format: DownloadFormat) => void
}

function RotateRightIcon() {
  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" className={styles.icon}>
      <path
        d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865A8.25 8.25 0 0 1 17.803 6.17l3.181 3.178"
        fill="none"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.7"
      />
    </svg>
  )
}

export function ActionBar({
  canUndo,
  canRedo,
  canRevert,
  canRotate,
  canDownload,
  isBusy,
  downloadFormat,
  onUndo,
  onRedo,
  onRevert,
  onRotateRight,
  onDownload,
  onDownloadFormatChange,
}: ActionBarProps) {
  return (
    <section className={styles.bar}>
      <div className={styles.actions}>
        <button type="button" disabled={!canUndo || isBusy} onClick={() => onUndo()}>
          Undo
        </button>
        <button type="button" disabled={!canRedo || isBusy} onClick={() => onRedo()}>
          Redo
        </button>
        <button type="button" disabled={!canRevert || isBusy} onClick={() => onRevert()}>
          Vrati original
        </button>
        <button
          type="button"
          className={styles.iconButton}
          aria-label="Rotiraj desno"
          title="Rotiraj desno"
          disabled={!canRotate || isBusy}
          onClick={() => onRotateRight()}
        >
          <RotateRightIcon />
        </button>
      </div>

      <div className={styles.download}>
        <label htmlFor="download-format">Format</label>
        <select
          id="download-format"
          value={downloadFormat}
          disabled={!canDownload || isBusy}
          onChange={(event: ChangeEvent<HTMLSelectElement>) =>
            onDownloadFormatChange(event.currentTarget.value as DownloadFormat)
          }
        >
          {DOWNLOAD_FORMATS.map((format) => (
            <option key={format.value} value={format.value}>
              {format.label}
            </option>
          ))}
        </select>
        <button type="button" disabled={!canDownload || isBusy} onClick={() => onDownload()}>
          Download
        </button>
      </div>
    </section>
  )
}
