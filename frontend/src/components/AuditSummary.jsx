import React, { useEffect, useState } from 'react'
import axios from 'axios'

axios.defaults.baseURL = '/api'

export default function AuditSummary({ auditId }) {
  const [summary, setSummary] = useState(null)

  useEffect(() => {
    axios.get(`/audit/${auditId}/summary`).then(res => setSummary(res.data))
  }, [auditId])

  if (!summary) return <p>No summary cached yet.</p>

  return (
    <div style={{ marginTop: '2rem' }}>
      <h2>Audit Summary</h2>
      <p><strong>Title:</strong> {summary.title}</p>
      <p><strong>Date:</strong> {new Date(summary.scheduledDate).toLocaleDateString()}</p>
      <p><strong>Assigned To:</strong> {summary.assignedTo}</p>
      <p><strong>Cached At:</strong> {new Date(summary.cachedAt).toLocaleTimeString()}</p>
    </div>
  )
}
