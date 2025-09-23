import React, { useEffect, useState } from 'react'
import axios from 'axios'

axios.defaults.baseURL = '/api'

export default function AuditList({ onSelectAudit }) {
  const [audits, setAudits] = useState([])

  useEffect(() => {
    axios.get('/audit').then(res => setAudits(res.data))
  }, [])

  return (
    <div>
      <h2>Audits</h2>
      <ul>
        {audits.map(a => (
          <li key={a.id}>
            {a.title} — {new Date(a.scheduledDate).toLocaleDateString()}
            <button onClick={() => onSelectAudit(a.id)} style={{ marginLeft: '0.5rem' }}>Summary</button>
          </li>
        ))}
      </ul>
    </div>
  )
}
