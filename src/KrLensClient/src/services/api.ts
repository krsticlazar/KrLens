import type { DownloadFormat, FilterName, FilterParameters, UploadResponse } from '../types'

const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

async function readError(response: Response): Promise<string> {
  const message = await response.text()
  return message || `${response.status} ${response.statusText}`
}

export async function uploadImage(file: File): Promise<UploadResponse> {
  const form = new FormData()
  form.append('file', file)

  const response = await fetch(`${BASE_URL}/api/image/upload`, {
    method: 'POST',
    body: form,
  })

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  return response.json() as Promise<UploadResponse>
}

export async function applyFilter(
  sessionId: string,
  filter: FilterName,
  parameters: FilterParameters,
): Promise<Blob> {
  const response = await fetch(`${BASE_URL}/api/filter/apply`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ sessionId, filter, parameters }),
  })

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  return response.blob()
}

export async function downloadImage(sessionId: string, format: DownloadFormat): Promise<Blob> {
  const response = await fetch(`${BASE_URL}/api/image/download/${sessionId}?format=${format}`)
  if (!response.ok) {
    throw new Error(await readError(response))
  }

  return response.blob()
}

export async function deleteSession(sessionId: string): Promise<void> {
  const response = await fetch(`${BASE_URL}/api/image/session/${sessionId}`, { method: 'DELETE' })
  if (!response.ok) {
    throw new Error(await readError(response))
  }
}
