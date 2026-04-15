import { startTransition, useEffect, useRef, useState } from 'react'
import { UploadPanel } from './components/UploadPanel/UploadPanel'
import { FilterPanel } from './components/FilterPanel/FilterPanel'
import { PreviewPanel } from './components/PreviewPanel/PreviewPanel'
import { ActionBar } from './components/ActionBar/ActionBar'
import { useFilterApi } from './hooks/useFilterApi'
import { useImageHistory } from './hooks/useImageHistory'
import type {
  AppState,
  DownloadFormat,
  FilterName,
  FilterParameters,
  ImageSnapshot,
} from './types'
import styles from './App.module.css'

const INITIAL_STATE: AppState = {
  sessionId: null,
  originalImageUrl: null,
  currentImageUrl: null,
  isLoading: false,
  error: null,
}

function stripExtension(fileName: string): string {
  return fileName.replace(/\.[^.]+$/, '') || 'krlens'
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Došlo je do neočekivane greške.'
}

export default function App() {
  const api = useFilterApi()
  const history = useImageHistory(null)
  const [appState, setAppState] = useState<AppState>(INITIAL_STATE)
  const [downloadFormat, setDownloadFormat] = useState<DownloadFormat>('png')

  const sessionIdRef = useRef<string | null>(null)
  const originalSnapshotRef = useRef<ImageSnapshot | null>(null)
  const managedUrlsRef = useRef<Set<string>>(new Set())

  const clearManagedUrls = () => {
    managedUrlsRef.current.forEach((url) => {
      if (typeof URL.revokeObjectURL === 'function') {
        URL.revokeObjectURL(url)
      }
    })
    managedUrlsRef.current.clear()
  }

  const rememberSnapshot = (blob: Blob, name: string): ImageSnapshot => {
    const url = URL.createObjectURL(blob)
    managedUrlsRef.current.add(url)
    return {
      url,
      blob,
      name,
      type: blob.type || 'image/png',
    }
  }

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
      clearManagedUrls()
      void safeDeleteSession(sessionIdRef.current)
    }
  }, [])

  const syncAppState = (sessionId: string | null, current: ImageSnapshot | null, loading = false, error: string | null = null) => {
    startTransition(() => {
      setAppState({
        sessionId,
        originalImageUrl: originalSnapshotRef.current?.url ?? null,
        currentImageUrl: current?.url ?? null,
        isLoading: loading,
        error,
      })
    })
  }

  const replaceSessionWithSnapshot = async (snapshot: ImageSnapshot) => {
    const previousSessionId = sessionIdRef.current
    const file = new File([snapshot.blob], snapshot.name, { type: snapshot.type || 'image/png' })
    const upload = await api.uploadImage(file)
    sessionIdRef.current = upload.sessionId

    if (previousSessionId && previousSessionId !== upload.sessionId) {
      void safeDeleteSession(previousSessionId)
    }

    return upload.sessionId
  }

  const handleUpload = async (file: File) => {
    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    let freshSessionId: string | null = null

    try {
      const upload = await api.uploadImage(file)
      freshSessionId = upload.sessionId
      const previewBlob = await api.downloadImage(upload.sessionId, 'png')
      const previousSessionId = sessionIdRef.current

      clearManagedUrls()

      const snapshot = rememberSnapshot(previewBlob, `${stripExtension(file.name)}.png`)
      originalSnapshotRef.current = snapshot
      history.reset(snapshot)
      sessionIdRef.current = upload.sessionId
      syncAppState(upload.sessionId, snapshot)

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

  const handleApply = async (filter: FilterName, parameters: FilterParameters) => {
    if (!sessionIdRef.current) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      const blob = await api.applyFilter(sessionIdRef.current, filter, parameters)
      const snapshot = rememberSnapshot(blob, `${filter.toLowerCase()}.png`)
      history.push(snapshot)
      syncAppState(sessionIdRef.current, snapshot)
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleUndo = async () => {
    const target = history.peekUndo
    if (!target) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      const sessionId = await replaceSessionWithSnapshot(target)
      history.undo()
      syncAppState(sessionId, target)
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleRedo = async () => {
    const target = history.peekRedo
    if (!target) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      const sessionId = await replaceSessionWithSnapshot(target)
      history.redo()
      syncAppState(sessionId, target)
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleRevert = async () => {
    const target = originalSnapshotRef.current
    if (!target) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      const sessionId = await replaceSessionWithSnapshot(target)
      history.revert(target)
      syncAppState(sessionId, target)
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const handleDownload = async () => {
    if (!sessionIdRef.current) {
      return
    }

    setAppState((current) => ({ ...current, isLoading: true, error: null }))

    try {
      const blob = await api.downloadImage(sessionIdRef.current, downloadFormat)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `krlens-${Date.now()}.${downloadFormat}`
      anchor.click()
      if (typeof URL.revokeObjectURL === 'function') {
        URL.revokeObjectURL(url)
      }
      setAppState((current) => ({ ...current, isLoading: false, error: null }))
    } catch (error) {
      setAppState((current) => ({
        ...current,
        isLoading: false,
        error: getErrorMessage(error),
      }))
    }
  }

  const hasSession = Boolean(appState.sessionId)
  const canRevert =
    Boolean(originalSnapshotRef.current) &&
    originalSnapshotRef.current?.url !== history.current?.url

  return (
    <main className={styles.shell}>
      <header className={styles.masthead}>
        <div className={styles.titleBlock}>
          <p className={styles.eyebrow}>KrLens</p>
          <h1>Multimedijalni studio za server-side obradu slike</h1>
          <p>
            Jedan kadar ulazi u sesiju, filteri se izvršavaju na backendu, a klijent čuva lokalnu
            istoriju za brzi povratak na prethodna stanja.
          </p>
        </div>
        <div className={styles.statusCard}>
          <span>Aktivna sesija</span>
          <strong>{appState.sessionId ? appState.sessionId.slice(0, 8) : 'nema'}</strong>
          <small>{appState.currentImageUrl ? 'Preview je spreman za dalju obradu.' : 'Upload čeka prvi kadar.'}</small>
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
              isLoading={appState.isLoading}
              error={appState.error}
            />
          </div>
          <ActionBar
            canUndo={history.canUndo}
            canRedo={history.canRedo}
            canRevert={canRevert}
            canDownload={hasSession}
            isBusy={appState.isLoading}
            downloadFormat={downloadFormat}
            onUndo={handleUndo}
            onRedo={handleRedo}
            onRevert={handleRevert}
            onDownload={handleDownload}
            onDownloadFormatChange={setDownloadFormat}
          />
        </div>
      </section>
    </main>
  )
}
