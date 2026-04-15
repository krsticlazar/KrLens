import type { ChangeEvent } from 'react'
import styles from './ActionBar.module.css'
import { DOWNLOAD_FORMATS, type DownloadFormat } from '../../types'

interface ActionBarProps {
  canUndo: boolean
  canRedo: boolean
  canRevert: boolean
  canDownload: boolean
  isBusy: boolean
  downloadFormat: DownloadFormat
  onUndo: () => Promise<void> | void
  onRedo: () => Promise<void> | void
  onRevert: () => Promise<void> | void
  onDownload: () => Promise<void> | void
  onDownloadFormatChange: (format: DownloadFormat) => void
}

export function ActionBar({
  canUndo,
  canRedo,
  canRevert,
  canDownload,
  isBusy,
  downloadFormat,
  onUndo,
  onRedo,
  onRevert,
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
