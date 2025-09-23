import React, { useState } from 'react'
import AuditList from './components/AuditList.jsx'
import AuditForm from './components/AuditForm.jsx'
import AuditSummary from './components/AuditSummary.jsx'

export default function App() {
  const [selectedAudit, setSelectedAudit] = useState(null)

  return (
    <div style={{ padding: '2rem', fontFamily: 'sans-serif' }}>
      <h1>RegulaFlow Audit SaaS</h1>
      <AuditForm />
      <AuditList onSelectAudit={setSelectedAudit} />
      {selectedAudit && <AuditSummary auditId={selectedAudit} />}
    </div>
  )
}
