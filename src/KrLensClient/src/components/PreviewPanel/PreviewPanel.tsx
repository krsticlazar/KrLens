import styles from './PreviewPanel.module.css'

interface PreviewPanelProps {
  originalImageUrl: string | null
  currentImageUrl: string | null
  isLoading: boolean
  error: string | null
}

export function PreviewPanel({
  originalImageUrl,
  currentImageUrl,
  isLoading,
  error,
}: PreviewPanelProps) {
  return (
    <section className={styles.panel}>
      <div className={styles.header}>
        <div>
          <p className={styles.eyebrow}>Preview</p>
          <h2>Radna slika</h2>
        </div>
        <p className={styles.copy}>Aktivni rezultat zauzima glavni kadar, dok original ostaje kao referenca.</p>
      </div>

      <div className={styles.stage}>
        {currentImageUrl ? (
          <img className={styles.mainImage} src={currentImageUrl} alt="Trenutni preview" />
        ) : (
          <div className={styles.placeholder}>
            <strong>Nema učitane slike</strong>
            <span>Počni upload-om sa leve strane da bi video preview i aktivirao filtere.</span>
          </div>
        )}

        {originalImageUrl && (
          <div className={styles.originalCard}>
            <span>Original</span>
            <img className={styles.originalImage} src={originalImageUrl} alt="Originalni preview" />
          </div>
        )}

        {isLoading && (
          <div className={styles.overlay} role="status" aria-live="polite">
            <div className={styles.spinner} />
            <span>Server obrađuje sliku...</span>
          </div>
        )}
      </div>

      {error && <p className={styles.error}>{error}</p>}
    </section>
  )
}
