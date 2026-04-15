import type { FilterDefinition, FilterName, FilterParameters } from '../../types'
import styles from './FilterPanel.module.css'

interface FilterItemProps {
  definition: FilterDefinition
  selected: boolean
  values: FilterParameters
  disabled: boolean
  onSelect: (name: FilterName) => void
  onValueChange: (name: FilterName, key: string, value: number) => void
  onApply: (name: FilterName, parameters: FilterParameters) => Promise<void> | void
}

export function FilterItem({
  definition,
  selected,
  values,
  disabled,
  onSelect,
  onValueChange,
  onApply,
}: FilterItemProps) {
  return (
    <article className={`${styles.item} ${selected ? styles.itemSelected : ''}`}>
      <button
        type="button"
        className={styles.itemToggle}
        onClick={() => onSelect(definition.name)}
        aria-expanded={selected}
      >
        <span>{definition.label}</span>
        <span className={styles.itemMeta}>{definition.params.length === 0 ? 'bez parametara' : 'parametri'}</span>
      </button>

      {selected && (
        <div className={styles.itemBody}>
          {definition.params.length === 0 ? (
            <p className={styles.emptyState}>Ovaj filter nema dodatne parametre.</p>
          ) : (
            <div className={styles.paramGrid}>
              {definition.params.map((param) => {
                const inputId = `${definition.name}-${param.key}`
                const value = values[param.key] ?? param.default

                if (param.type === 'slider') {
                  return (
                    <label className={styles.paramField} htmlFor={inputId} key={param.key}>
                      <span className={styles.paramLabel}>
                        {param.label}
                        <strong>{value}</strong>
                      </span>
                      <input
                        id={inputId}
                        type="range"
                        min={param.min}
                        max={param.max}
                        step={param.step}
                        value={value}
                        onChange={(event) =>
                          onValueChange(definition.name, param.key, Number(event.currentTarget.value))
                        }
                      />
                    </label>
                  )
                }

                if (param.type === 'select') {
                  return (
                    <label className={styles.paramField} htmlFor={inputId} key={param.key}>
                      <span className={styles.paramLabel}>{param.label}</span>
                      <select
                        id={inputId}
                        value={value}
                        onChange={(event) =>
                          onValueChange(definition.name, param.key, Number(event.currentTarget.value))
                        }
                      >
                        {param.options?.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </label>
                  )
                }

                return (
                  <label className={styles.paramField} htmlFor={inputId} key={param.key}>
                    <span className={styles.paramLabel}>{param.label}</span>
                    <input
                      id={inputId}
                      type="number"
                      min={param.min}
                      max={param.max}
                      step={param.step}
                      value={value}
                      onChange={(event) =>
                        onValueChange(definition.name, param.key, Number(event.currentTarget.value))
                      }
                    />
                  </label>
                )
              })}
            </div>
          )}

          <button
            type="button"
            className={styles.applyButton}
            disabled={disabled}
            onClick={() => onApply(definition.name, values)}
          >
            Primeni filter
          </button>
        </div>
      )}
    </article>
  )
}
