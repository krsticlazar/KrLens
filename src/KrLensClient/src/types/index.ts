export type FilterName =
  | 'Grayscale'
  | 'Invert'
  | 'Brightness'
  | 'Contrast'
  | 'Gamma'
  | 'Smooth'
  | 'EdgeDetectHV'
  | 'Flip'
  | 'Water'
  | 'Stucki'
  | 'HistogramEqualizing'

export type DownloadFormat = 'png' | 'jpeg' | 'bmp' | 'gif' | 'msi'

export interface ParamOption {
  label: string
  value: number
}

export interface ParamDefinition {
  key: string
  label: string
  type: 'slider' | 'select' | 'number'
  min?: number
  max?: number
  step?: number
  default: number
  options?: ParamOption[]
}

export interface FilterDefinition {
  name: FilterName
  label: string
  params: ParamDefinition[]
}

export interface OriginalImageMeta {
  format: string
  width: number
  height: number
}

export interface AppState {
  sessionId: string | null
  originalImageUrl: string | null
  currentImageUrl: string | null
  originalMeta: OriginalImageMeta | null
  sessionState: SessionState | null
  isLoading: boolean
  error: string | null
}

export interface SessionHistoryItem {
  step: number
  filter: string
  parameters: FilterParameters | null
}

export interface SessionState {
  sessionId: string
  width: number
  height: number
  currentStep: number
  maxHistory: number
  canUndo: boolean
  canRedo: boolean
  history: SessionHistoryItem[]
}

export interface UploadResponse {
  sessionId: string
  width: number
  height: number
  state: SessionState
}

export interface ImageSnapshot {
  url: string
  blob: Blob
  name: string
  type: string
}

export type FilterParameters = Record<string, number>
export type FilterFormState = Record<FilterName, FilterParameters>

export const FILTER_DEFINITIONS: FilterDefinition[] = [
  { name: 'Grayscale', label: 'Grayscale', params: [] },
  { name: 'Invert', label: 'Invert', params: [] },
  {
    name: 'Brightness',
    label: 'Brightness',
    params: [{ key: 'delta', label: 'Delta', type: 'slider', min: -255, max: 255, step: 1, default: 0 }],
  },
  {
    name: 'Contrast',
    label: 'Contrast',
    params: [{ key: 'factor', label: 'Factor', type: 'slider', min: 0, max: 3, step: 0.1, default: 1 }],
  },
  {
    name: 'Gamma',
    label: 'Gamma',
    params: [{ key: 'gamma', label: 'Gamma', type: 'slider', min: 0.1, max: 5, step: 0.1, default: 1 }],
  },
  {
    name: 'Smooth',
    label: 'Smooth',
    params: [{ key: 'radius', label: 'Radius', type: 'slider', min: 1, max: 5, step: 1, default: 1 }],
  },
  {
    name: 'EdgeDetectHV',
    label: 'Edge Detect H/V',
    params: [
      {
        key: 'direction',
        label: 'Direction',
        type: 'select',
        default: 2,
        options: [
          { label: 'Horizontal', value: 0 },
          { label: 'Vertical', value: 1 },
          { label: 'Oba', value: 2 },
        ],
      },
    ],
  },
  {
    name: 'Flip',
    label: 'Flip H/V',
    params: [
      {
        key: 'axis',
        label: 'Axis',
        type: 'select',
        default: 0,
        options: [
          { label: 'Horizontal', value: 0 },
          { label: 'Vertical', value: 1 },
        ],
      },
    ],
  },
  {
    name: 'Water',
    label: 'Water',
    params: [
      { key: 'amplitude', label: 'Amplitude', type: 'number', min: 0, max: 100, step: 1, default: 5 },
      { key: 'wavelength', label: 'Wavelength', type: 'number', min: 1, max: 1000, step: 1, default: 20 },
    ],
  },
  { name: 'Stucki', label: 'Stucki', params: [] },
  { name: 'HistogramEqualizing', label: 'Histogram Equalizing', params: [] },
]

export const DOWNLOAD_FORMATS: Array<{ label: string; value: DownloadFormat }> = [
  { label: 'PNG', value: 'png' },
  { label: 'JPEG', value: 'jpeg' },
  { label: 'BMP', value: 'bmp' },
  { label: 'GIF', value: 'gif' },
  { label: 'MSI', value: 'msi' },
]
