import { useState, useEffect } from 'react';

function App() {
  const [token, setToken] = useState(localStorage.getItem('token') || '');
  const [role, setRole] = useState(localStorage.getItem('role') || '');
  const [email, setEmail] = useState(localStorage.getItem('email') || '');
  const [password, setPassword] = useState('');

  const [assignments, setAssignments] = useState([]);
  const [submissions, setSubmissions] = useState([]);
  const [mySubmissions, setMySubmissions] = useState([]);
  const [users, setUsers] = useState([]);

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [maxMarks, setMaxMarks] = useState(100);

  const handleLogin = async (e) => {
    e.preventDefault();
    const res = await fetch('http://localhost:5000/api/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password })
    });

    if (res.ok) {
      const data = await res.json();
      setToken(data.token);
      setRole(data.role);
      setEmail(data.email);
      localStorage.setItem('token', data.token);
      localStorage.setItem('role', data.role);
      localStorage.setItem('email', data.email);
    } else {
      alert('Invalid login credentials');
    }
  };

  const handleLogout = () => {
    setToken('');
    setRole('');
    setEmail('');
    localStorage.clear();
  };

  const fetchData = async () => {
    if (!token) return;
    const authHeaders = { 'Authorization': `Bearer ${token}` };

    const assignRes = await fetch('http://localhost:5000/api/assignments', { headers: authHeaders });
    if (assignRes.ok) setAssignments(await assignRes.json());

    if (role === 'Teacher' || role === 'Admin') {
      const subRes = await fetch('http://localhost:5000/api/submissions', { headers: authHeaders });
      if (subRes.ok) setSubmissions(await subRes.json());
    }

    if (role === 'Student') {
      const mySubRes = await fetch('http://localhost:5000/api/submissions/my', { headers: authHeaders });
      if (mySubRes.ok) setMySubmissions(await mySubRes.json());
    }

    if (role === 'Admin') {
      const userRes = await fetch('http://localhost:5000/api/admin/users', { headers: authHeaders });
      if (userRes.ok) setUsers(await userRes.json());
    }
  };

  useEffect(() => {
    fetchData();
  }, [token, role]);

  const handleCreateAssignment = async (e) => {
    e.preventDefault();
    await fetch('http://localhost:5000/api/assignments', {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ title, description, maxMarks: parseInt(maxMarks) })
    });
    setTitle('');
    setDescription('');
    fetchData();
  };

  const handleSubmitAnswer = async (assignmentId) => {
    const answer = prompt("Enter your submission text:");
    if (!answer) return;

    await fetch('http://localhost:5000/api/submissions', {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ assignmentId, answerText: answer })
    });
    alert('Submitted successfully!');
    fetchData();
  };

  const handleGradeSubmission = async (subId) => {
    const marks = prompt("Enter Marks:");
    const feedback = prompt("Enter Teacher Feedback:");
    if (!marks || !feedback) return;

    await fetch(`http://localhost:5000/api/submissions/${subId}/grade`, {
      method: 'PUT',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ marks: parseInt(marks), feedback })
    });
    alert('Graded successfully!');
    fetchData();
  };

  if (!token) {
    return (
      <div className="auth-container">
        <div className="card auth-card">
          <h2 style={{ marginBottom: '8px' }}>Sign In</h2>
          <p style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '20px' }}>
            Enter your credentials to access the portal
          </p>
          <form onSubmit={handleLogin}>
            <div className="form-group">
              <label>Email Address</label>
              <input type="email" value={email} onChange={e => setEmail(e.target.value)} required placeholder="name@school.com" />
            </div>
            <div className="form-group">
              <label>Password</label>
              <input type="password" value={password} onChange={e => setPassword(e.target.value)} required placeholder="••••••••" />
            </div>
            <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '10px' }}>
              Sign In
            </button>
          </form>

          <div style={{ marginTop: '24px', paddingTop: '16px', borderTop: '1px solid var(--border)', fontSize: '12px', color: 'var(--text-muted)' }}>
            <strong>Demo Logins:</strong><br/>
            Admin: <code>admin@school.com</code> / <code>AdminPassword123!</code><br/>
            Teacher: <code>teacher@school.com</code> / <code>TeacherPassword123!</code><br/>
            Student: <code>student@school.com</code> / <code>StudentPassword123!</code>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div>
      <nav className="navbar">
        <div>
          <h3 style={{ fontSize: '18px' }}>School Portal</h3>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <span style={{ fontSize: '14px', color: 'var(--text-muted)' }}>
            {email} <span className={`badge badge-${role}`}>{role}</span>
          </span>
          <button onClick={handleLogout} className="btn btn-danger btn-sm">Logout</button>
        </div>
      </nav>

      <main className="app-container">
        {/* ADMIN VIEW */}
        {role === 'Admin' && (
          <div className="card">
            <h3>User Management</h3>
            <p style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '16px' }}>Registered accounts across the institution</p>
            <table className="table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Email</th>
                  <th>Role</th>
                </tr>
              </thead>
              <tbody>
                {users.map(u => (
                  <tr key={u.id}>
                    <td>#{u.id}</td>
                    <td>{u.email}</td>
                    <td><span className={`badge badge-${u.role}`}>{u.role}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* TEACHER CREATE ASSIGNMENT */}
        {role === 'Teacher' && (
          <div className="card">
            <h3>Publish New Assignment</h3>
            <form onSubmit={handleCreateAssignment} style={{ marginTop: '16px' }}>
              <div className="grid-2">
                <div className="form-group">
                  <label>Title</label>
                  <input type="text" value={title} onChange={e => setTitle(e.target.value)} required placeholder="e.g. Midterm Essay" />
                </div>
                <div className="form-group">
                  <label>Max Marks</label>
                  <input type="number" value={maxMarks} onChange={e => setMaxMarks(e.target.value)} required />
                </div>
              </div>
              <div className="form-group">
                <label>Instructions & Description</label>
                <input type="text" value={description} onChange={e => setDescription(e.target.value)} required placeholder="Provide clear submission details..." />
              </div>
              <button type="submit" className="btn btn-primary">Publish Assignment</button>
            </form>
          </div>
        )}

        {/* TEACHER SUBMISSION GRADING */}
        {(role === 'Teacher' || role === 'Admin') && (
          <div className="card">
            <h3>Submissions Queue</h3>
            <table className="table">
              <thead>
                <tr>
                  <th>Student</th>
                  <th>Answer</th>
                  <th>Status</th>
                  <th>Marks</th>
                  <th>Feedback</th>
                  {role === 'Teacher' && <th>Action</th>}
                </tr>
              </thead>
              <tbody>
                {submissions.length === 0 ? (
                  <tr><td colSpan="6" style={{ textAlign: 'center', color: 'var(--text-muted)' }}>No submissions found.</td></tr>
                ) : submissions.map(s => (
                  <tr key={s.id}>
                    <td>{s.studentEmail}</td>
                    <td>{s.answerText}</td>
                    <td><span className={`badge badge-${s.status}`}>{s.status}</span></td>
                    <td>{s.marks !== null ? `${s.marks} marks` : 'Unassigned'}</td>
                    <td>{s.feedback || 'No feedback'}</td>
                    {role === 'Teacher' && (
                      <td>
                        <button onClick={() => handleGradeSubmission(s.id)} className="btn btn-primary btn-sm">Grade</button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* ASSIGNMENTS LIST */}
        <div className="card">
          <h3>Active Assignments</h3>
          <div style={{ display: 'grid', gap: '16px', marginTop: '16px' }}>
            {assignments.length === 0 ? (
              <p style={{ color: 'var(--text-muted)', fontSize: '14px' }}>No active assignments available.</p>
            ) : assignments.map(a => (
              <div key={a.id} style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '16px', background: 'var(--bg-main)' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                  <div>
                    <h4 style={{ fontSize: '16px' }}>{a.title}</h4>
                    <p style={{ fontSize: '14px', color: 'var(--text-muted)', marginTop: '4px' }}>{a.description}</p>
                  </div>
                  <span className="badge" style={{ background: '#e2e8f0' }}>Max Marks: {a.maxMarks}</span>
                </div>
                {role === 'Student' && (
                  <button onClick={() => handleSubmitAnswer(a.id)} className="btn btn-primary btn-sm" style={{ marginTop: '12px' }}>
                    Submit Answer
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* STUDENT SUBMISSION HISTORY */}
        {role === 'Student' && (
          <div className="card">
            <h3>My Submissions & Results</h3>
            <table className="table">
              <thead>
                <tr>
                  <th>Assignment ID</th>
                  <th>Status</th>
                  <th>Grade</th>
                  <th>Teacher Feedback</th>
                </tr>
              </thead>
              <tbody>
                {mySubmissions.length === 0 ? (
                  <tr><td colSpan="4" style={{ textAlign: 'center', color: 'var(--text-muted)' }}>You have not submitted any assignments yet.</td></tr>
                ) : mySubmissions.map(s => (
                  <tr key={s.id}>
                    <td>Assignment #{s.assignmentId}</td>
                    <td><span className={`badge badge-${s.status}`}>{s.status}</span></td>
                    <td>{s.marks !== null ? `${s.marks} pts` : 'Pending'}</td>
                    <td>{s.feedback || 'Awaiting teacher feedback...'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;