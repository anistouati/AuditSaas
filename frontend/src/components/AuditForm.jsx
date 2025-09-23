import React, { useState } from 'react'
import axios from 'axios'

axios.defaults.baseURL = '/api'

export default function AuditForm() {
  const [title, setTitle] = useState('')
  const [date, setDate] = useState('')
  const [assignedTo, setAssignedTo] = useState('')

  const submit = async (e) => {
    e.preventDefault()
    await axios.post('/audit/schedule', {
      title,
      scheduledDate: date,
      assignedTo
    })
    setTitle('')
    setDate('')
    setAssignedTo('')
    alert('Audit scheduled')
  }

  return (
    <form onSubmit={submit} style={{ marginBottom: '2rem' }}>
      <h2>Schedule Audit</h2>
      <input placeholder="Title" value={title} onChange={e => setTitle(e.target.value)} required />
      <input type="date" value={date} onChange={e => setDate(e.target.value)} required />
      <input placeholder="Assigned To" value={assignedTo} onChange={e => setAssignedTo(e.target.value)} required />
      <button type="submit">Schedule</button>
    </form>
  )
}
