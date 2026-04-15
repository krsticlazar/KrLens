import { useState } from 'react'
import styles from './UploadPanel.module.css'

interface UploadPanelProps {
  disabled: boolean
  isLoading: boolean
  onUpload: (file: File) => Promise<void> | void
}

const ACCEPTED_FORMATS = '.png,.jpg,.jpeg,.bmp,.gif,.msi'

export function UploadPanel({ disabled, isLoading, onUpload }: UploadPanelProps) {
  const [isDragActive, setIsDragActive] = useState(false)
  const [fileName, setFileName] = useState<string | null>(null)

  const handleFile = (file: File | null) => {
    if (!file) {
      return
    }

    setFileName(file.name)
    void onUpload(file)
  }

  return (
    <section className={styles.panel}>
      <div className={styles.copy}>
        <p className={styles.eyebrow}>Ulaz</p>
        <h2>Učitaj sliku</h2>
        <p>
          Podržani su PNG, JPEG, BMP, GIF i MSI. Posle uploada backend drži radnu sesiju za sva
          sledeća procesiranja.
        </p>
      </div>

      <label
        className={`${styles.dropzone} ${isDragActive ? styles.dropzoneActive : ''}`}
        htmlFor="image-upload"
        onDragOver={(event) => {
          event.preventDefault()
          if (!disabled) {
            setIsDragActive(true)
          }
        }}
        onDragLeave={() => setIsDragActive(false)}
        onDrop={(event) => {
          event.preventDefault()
          setIsDragActive(false)
          if (!disabled) {
            handleFile(event.dataTransfer.files?.[0] ?? null)
          }
        }}
      >
        <input
          id="image-upload"
          aria-label="Učitaj sliku"
          type="file"
          accept={ACCEPTED_FORMATS}
          className={styles.input}
          disabled={disabled}
          onChange={(event) => {
            handleFile(event.target.files?.[0] ?? null)
            event.currentTarget.value = ''
          }}
        />
        <span className={styles.badge}>{isLoading ? 'Obrada...' : 'Drop ili klik'}</span>
        <strong>Pošalji novi kadar</strong>
        <span>{fileName ?? 'Prevuci fajl ili otvori sistemski picker.'}</span>
      </label>
    </section>
  )
}
