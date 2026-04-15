import { useState } from 'react'
import { FilterItem } from './FilterItem'
import styles from './FilterPanel.module.css'
import { FILTER_DEFINITIONS, type FilterFormState, type FilterName, type FilterParameters } from '../../types'

interface FilterPanelProps {
  disabled: boolean
  onApply: (name: FilterName, parameters: FilterParameters) => Promise<void> | void
}

function buildInitialState(): FilterFormState {
  return FILTER_DEFINITIONS.reduce<FilterFormState>((state, definition) => {
    state[definition.name] = definition.params.reduce<FilterParameters>((params, param) => {
      params[param.key] = param.default
      return params
    }, {})
    return state
  }, {} as FilterFormState)
}

export function FilterPanel({ disabled, onApply }: FilterPanelProps) {
  const [selectedFilter, setSelectedFilter] = useState<FilterName>(FILTER_DEFINITIONS[0].name)
  const [values, setValues] = useState<FilterFormState>(() => buildInitialState())

  return (
    <section className={styles.panel}>
      <div className={styles.copy}>
        <p className={styles.eyebrow}>Filteri</p>
        <h2>Lanac obrade</h2>
        <p>Izaberi efekat, podesi parametre i pošalji zahtev serveru. Obrada se radi nad aktivnom sesijom.</p>
      </div>

      <div className={styles.list}>
        {FILTER_DEFINITIONS.map((definition) => (
          <FilterItem
            key={definition.name}
            definition={definition}
            selected={selectedFilter === definition.name}
            values={values[definition.name]}
            disabled={disabled}
            onSelect={setSelectedFilter}
            onValueChange={(filterName, key, value) => {
              setValues((current) => ({
                ...current,
                [filterName]: {
                  ...current[filterName],
                  [key]: value,
                },
              }))
            }}
            onApply={onApply}
          />
        ))}
      </div>
    </section>
  )
}
