import { startTransition, useEffect, useRef, useState } from 'react'
import { UploadPanel } from './components/UploadPanel/UploadPanel'
import { FilterPanel } from './components/FilterPanel/FilterPanel'
import { PreviewPanel } from './components/PreviewPanel/PreviewPanel'
import { ActionBar } from './components/ActionBar/ActionBar'
import { useFilterApi } from './hooks/useFilterApi'
import type {
  AppState,
  DownloadFormat,
  FilterName,
  FilterParameters,
  ImageSnapshot,
  OriginalImageMeta,
  SessionState,
} from './types'
import styles from './App.module.css'

const INITIAL_STATE: AppState = {
  sessionId: null,
  originalImageUrl: null,
  currentImageUrl: null,
  originalMeta: null,
  sessionState: null,
  isLoading: false,
  error: null,
}

function stripExtension(fileName: string): string {
  return fileName.replace(/\.[^.]+$/, '') || 'krlens'
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Doslo je do neocekivane greske.'
}

function normalizeFormatLabel(value: string | null | undefined): string {
  if (!value) {
    return 'Nepoznat'
  }

  const normalized = value.trim().toLowerCase()

  if (normalized === 'jpg' || normalized === 'jpeg') {
    return 'JPEG'
  }

  return normalized.toUpperCase()
}

function getOriginalMeta(file: File, state: SessionState): OriginalImageMeta {
  const extension = file.name.includes('.') ? file.name.split('.').pop() : null
  const mimeSuffix = file.type.includes('/') ? file.type.split('/').pop() : null

  return {
    format: normalizeFormatLabel(extension ?? mimeSuffix),
    width: state.width,
    height: state.height,
  }
}

export default function App() {
  const api = useFilterApi()
  const [appState, setAppState] = useState<AppState>(INITIAL_STATE)
  const [downloadFormat, setDownloadFormat] = useState<DownloadFormat>('png')

  const sessionIdRef = useRef<string | null>(null)
  const originalSnapshotRef = useRef<ImageSnapshot | null>(null)
  const currentSnapshotRef = useRef<ImageSnapshot | null>(null)
  const originalMetaRef = useRef<OriginalImageMeta | null>(null)

  const revokeUrl = (url: string) => {
    if (typeof URL.revokeObjectURL === 'function') {
      URL.revokeObjectURL(url)
    }
  }

  const replaceSnapshots = (original: ImageSnapshot | null, current: ImageSnapshot | null) => {
    const previous = [originalSnapshotRef.current, currentSnapshotRef.current].filter(Boolean) as ImageSnapshot[]
    const preserved = new Set<string>()

    if (original) {
      preserved.add(original.url)
    }

    if (current) {
      preserved.add(current.url)
    }

    const released = new Set<string>()
    previous.forEach((snapshot) => {
      if (!released.has(snapshot.url) && !preserved.has(snapshot.url)) {
        revokeUrl(snapshot.url)
        released.add(snapshot.url)
      }
    })

    originalSnapshotRef.current = original
    currentSnapshotRef.current = current
  }

  const replaceCurrentSnapshot = (current: ImageSnapshot | null) => {
    replaceSnapshots(originalSnapshotRef.current, current)
  }

  const clearSnapshots = () => {
    replaceSnapshots(null, null)
  }

  const rememberSnapshot = (blob: Blob, name: string): ImageSnapshot => ({
    url: URL.createObjectURL(blob),
    blob,
    name,
    type: blob.type || 'image/png',
  })

  const safeDeleteSession = async (sessionId: string | null) => {
    if (!sessionId) {
      return
    }

    try {
      await api.deleteSession(sessionId)
    } catch {
      // Ignore stale cleanup failures.
    }
  }

  useEffect(() => {
    return () => {
      clearSnapshots()
      void safeDeleteSession(sessionIdRef.current)
    }
  }, [])

  const syncAppState = (
    sessionId: string | null,
    sessionState: SessionState | null,
    loading = false,
    error: string | null = null,
  ) => {
    startTransition(() => {
      setAppState({
        sessionId,
        originalImageUrl: originalSnapshotRef.current?.url ?? null,
        currentImageUrl: currentSnapshotRef.current?.url ?? null,
        originalMeta: originalMetaRef.current,
        sessionState,
        isLoading: loading,
        error,
      })
    })
  }

  const syncOriginalMetaDimensions = (state: SessionState) => {
    if (!originalMetaRef.current) {
      return
    }

    originalMetaRef.current = {
      ...originalMetaRef.current,
      width: state.width,
      height: state.height,
    }
  }

  const handleUpload = async (file: File) => {
    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    let freshSessionId: string | null = null

    try {
      const upload = await api.uploadImage(file)
      freshSessionId = upload.sessionId
      const previewBlob = await api.downloadImage(upload.sessionId, 'png')
      const previousSessionId = sessionIdRef.current
      const snapshot = rememberSnapshot(previewBlob, `${stripExtension(file.name)}.png`)

      replaceSnapshots(snapshot, snapshot)
      originalMetaRef.current = getOriginalMeta(file, upload.state)
      sessionIdRef.current = upload.sessionId
      syncAppState(upload.sessionId, upload.state)

      if (previousSessionId && previousSessionId !== upload.sessionId) {
        void safeDeleteSession(previousSessionId)
      }
    } catch (error) {
      if (freshSessionId && freshSessionId !== sessionIdRef.current) {
        void safeDeleteSession(freshSessionId)
      }

      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const updateSessionPreview = async (sessionId: string, blobPromise: Promise<Blob>, snapshotName: string) => {
    const blob = await blobPromise
    const state = await api.getSessionState(sessionId)
    const snapshot = rememberSnapshot(blob, snapshotName)

    replaceCurrentSnapshot(snapshot)
    syncOriginalMetaDimensions(state)
    syncAppState(sessionId, state)
  }

  const handleApply = async (filter: FilterName, parameters: FilterParameters) => {
    const sessionId = sessionIdRef.current
    if (!sessionId) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      await updateSessionPreview(sessionId, api.applyFilter(sessionId, filter, parameters), `${filter.toLowerCase()}.png`)
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleUndo = async () => {
    const sessionId = sessionIdRef.current
    if (!sessionId) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      await updateSessionPreview(sessionId, api.undoSession(sessionId), 'undo.png')
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleRedo = async () => {
    const sessionId = sessionIdRef.current
    if (!sessionId) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      await updateSessionPreview(sessionId, api.redoSession(sessionId), 'redo.png')
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleRevert = async () => {
    const sessionId = sessionIdRef.current
    if (!sessionId) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      await updateSessionPreview(sessionId, api.revertSession(sessionId), 'original.png')
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleRotateRight = async () => {
    const sessionId = sessionIdRef.current
    if (!sessionId) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      await updateSessionPreview(sessionId, api.rotateSession(sessionId), 'rotate-right.png')
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleDownload = async () => {
    const sessionId = sessionIdRef.current
    if (!sessionId) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      const blob = await api.downloadImage(sessionId, downloadFormat)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `krlens-${Date.now()}.${downloadFormat}`
      anchor.click()
      revokeUrl(url)
      setAppState((current) => ({ ...current, isLoading: false, error: null }))
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const sessionState = appState.sessionState
  const hasSession = Boolean(appState.sessionId && appState.currentImageUrl)
  const canRevert = Boolean(sessionState && sessionState.currentStep > 0)

  return (
    <main className={styles.shell}>
      <header className={styles.masthead}>
        <div className={styles.titleBlock}>
          <img className={styles.logo} src="/KrLens-Logo.svg" alt="KrLens logo" />
          <h1>Multimedijalni studio za server-side obradu slike</h1>
        </div>
      </header>

      <section className={styles.workspace}>
        <div className={styles.sideColumn}>
          <div className={styles.card}>
            <UploadPanel disabled={appState.isLoading} isLoading={appState.isLoading} onUpload={handleUpload} />
          </div>
          <div className={styles.card}>
            <FilterPanel disabled={!hasSession || appState.isLoading} onApply={handleApply} />
          </div>
        </div>

        <div className={styles.mainColumn}>
          <div className={styles.heroCard}>
            <PreviewPanel
              originalImageUrl={appState.originalImageUrl}
              currentImageUrl={appState.currentImageUrl}
              originalMeta={appState.originalMeta}
              isLoading={appState.isLoading}
              error={appState.error}
            />
          </div>

          <ActionBar
            canUndo={Boolean(sessionState?.canUndo)}
            canRedo={Boolean(sessionState?.canRedo)}
            canRevert={canRevert}
            canRotate={hasSession}
            canDownload={hasSession}
            isBusy={appState.isLoading}
            downloadFormat={downloadFormat}
            onUndo={handleUndo}
            onRedo={handleRedo}
            onRevert={handleRevert}
            onRotateRight={handleRotateRight}
            onDownload={handleDownload}
            onDownloadFormatChange={setDownloadFormat}
          />
        </div>
      </section>

      <footer className={styles.footer}>
        <p>All rights reserved. KrLens je open source projekat.</p>
        <a href="https://github.com/krsticlazar/KrLens" target="_blank" rel="noreferrer">
          https://github.com/krsticlazar/KrLens
        </a>
      </footer>
    </main>
  )
}
