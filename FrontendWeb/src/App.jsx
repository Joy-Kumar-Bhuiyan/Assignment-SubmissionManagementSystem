import { useState, useEffect, useCallback } from 'react';

// Backend port configuration
const API_BASE_URL = 'http://localhost:5000';

function App() {
  // Authentication State
  const [token, setToken] = useState(() => localStorage.getItem('token') || '');
  const [role, setRole] = useState(() => localStorage.getItem('role') || '');
  const [email, setEmail] = useState(() => localStorage.getItem('email') || '');
  const [password, setPassword] = useState('');

  // Domain Data State
  const [assignments, setAssignments] = useState([]);
  const [submissions, setSubmissions] = useState([]);
  const [mySubmissions, setMySubmissions] = useState([]);
  const [users, setUsers] = useState([]);

  // Form Input State (Assignment Creation)
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [maxMarks, setMaxMarks] = useState(100);

  // ---------------------------------------------------------------------------
  // Data Fetching
  // ---------------------------------------------------------------------------
  const fetchData = useCallback(async () => {
    if (!token) return;
    const authHeaders = { Authorization: `Bearer ${token}` };

    try {
      // 1. Fetch Active Assignments
      const assignRes = await fetch(`${API_BASE_URL}/api/assignments`, { headers: authHeaders });
      if (assignRes.ok) setAssignments(await assignRes.json());

      // 2. Fetch All Submissions (Teacher / Admin)
      if (role === 'Teacher' || role === 'Admin') {
        const subRes = await fetch(`${API_BASE_URL}/api/submissions`, { headers: authHeaders });
        if (subRes.ok) setSubmissions(await subRes.json());
      }

      // 3. Fetch Student's Personal Submissions
      if (role === 'Student') {
        const mySubRes = await fetch(`${API_BASE_URL}/api/submissions/my`, { headers: authHeaders });
        if (mySubRes.ok) {
          setMySubmissions(await mySubRes.json());
        } else {
          // Fallback endpoint check
          const altMySubRes = await fetch(`${API_BASE_URL}/api/submissions/my-submissions`, { headers: authHeaders });
          if (altMySubRes.ok) setMySubmissions(await altMySubRes.json());
        }
      }

      // 4. Fetch Users List (Admin)
      if (role === 'Admin') {
        const userRes = await fetch(`${API_BASE_URL}/api/users`, { headers: authHeaders });
        if (userRes.ok) {
          setUsers(await userRes.json());
        } else {
          const altUserRes = await fetch(`${API_BASE_URL}/api/admin/users`, { headers: authHeaders });
          if (altUserRes.ok) setUsers(await altUserRes.json());
        }
      }
    } catch (error) {
      console.error('Error fetching portal data:', error);
    }
  }, [token, role]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  // ---------------------------------------------------------------------------
  // Authentication Actions
  // ---------------------------------------------------------------------------
  const handleLogin = async (e) => {
    e.preventDefault();
    try {
      let res = await fetch(`${API_BASE_URL}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      });

      if (!res.ok) {
        res = await fetch(`${API_BASE_URL}/api/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password })
        });
      }

      if (res.ok) {
        const data = await res.json();
        setToken(data.token);
        setRole(data.role);
        setEmail(data.email);
        setPassword('');

        localStorage.setItem('token', data.token);
        localStorage.setItem('role', data.role);
        localStorage.setItem('email', data.email);
      } else {
        alert('Invalid login credentials. Please check your email and password.');
      }
    } catch (error) {
      console.error('Login request failed:', error);
      alert('Unable to connect to the backend server. Please check your API connection.');
    }
  };

  const handleLogout = () => {
    setToken('');
    setRole('');
    setEmail('');
    setPassword('');
    setAssignments([]);
    setSubmissions([]);
    setMySubmissions([]);
    setUsers([]);
    localStorage.clear();
  };

  // ---------------------------------------------------------------------------
  // Assignment Actions
  // ---------------------------------------------------------------------------
  const handleCreateAssignment = async (e) => {
    e.preventDefault();
    try {
      const response = await fetch(`${API_BASE_URL}/api/assignments`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ title, description, maxMarks: parseInt(maxMarks, 10) })
      });

      if (response.ok) {
        alert('Assignment published successfully!');
        setTitle('');
        setDescription('');
        setMaxMarks(100);
        fetchData();
      } else {
        alert('Failed to publish assignment.');
      }
    } catch (error) {
      console.error('Error creating assignment:', error);
      alert('Network error while creating assignment.');
    }
  };

  const handleDeleteAssignment = async (assignmentId) => {
    if (!window.confirm('Are you sure you want to delete this assignment?')) return;

    try {
      const response = await fetch(`${API_BASE_URL}/api/assignments/${assignmentId}`, {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`
        }
      });

      if (response.ok) {
        alert('Assignment deleted successfully!');
        fetchData();
      } else {
        alert('Failed to delete assignment.');
      }
    } catch (error) {
      console.error('Error deleting assignment:', error);
      alert('Network error while deleting assignment.');
    }
  };

  // ---------------------------------------------------------------------------
  // Submission & Grading Actions
  // ---------------------------------------------------------------------------
  const handleSubmitAnswer = async (assignmentId) => {
    const answer = prompt('Enter your submission text:');
    if (!answer || answer.trim() === '') return;

    try {
      const response = await fetch(`${API_BASE_URL}/api/submissions`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ assignmentId, answerText: answer })
      });

      if (response.ok) {
        alert('Submitted successfully!');
        fetchData();
      } else {
        alert('Failed to submit answer.');
      }
    } catch (error) {
      console.error('Error submitting answer:', error);
      alert('Network error while submitting answer.');
    }
  };

  const handleGradeSubmission = async (subId) => {
    const marks = prompt('Enter Marks:');
    if (marks === null) return;

    const feedback = prompt('Enter Teacher Feedback:');
    if (feedback === null) return;

    try {
      const response = await fetch(`${API_BASE_URL}/api/submissions/${subId}/grade`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ marks: parseInt(marks, 10), feedback })
      });

      if (response.ok) {
        alert('Graded successfully!');
        fetchData();
      } else {
        alert('Failed to grade submission.');
      }
    } catch (error) {
      console.error('Error grading submission:', error);
      alert('Network error while grading submission.');
    }
  };

  // ---------------------------------------------------------------------------
  // Student Computations
  // ---------------------------------------------------------------------------
  const submittedAssignmentIds = new Set(
    mySubmissions.map((sub) => sub.assignmentId)
  );

  const pendingAssignments = assignments.filter(
    (a) => !submittedAssignmentIds.has(a.id)
  );

  const completedSubmissions = mySubmissions.map((sub) => {
    const assignment = assignments.find((a) => a.id === sub.assignmentId);
    return {
      ...sub,
      assignmentTitle: assignment ? assignment.title : `Assignment #${sub.assignmentId}`,
      maxMarks: assignment ? assignment.maxMarks : '-'
    };
  });

  // ---------------------------------------------------------------------------
  // Unauthenticated Render (Login Screen)
  // ---------------------------------------------------------------------------
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
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                placeholder="name@school.com"
              />
            </div>
            <div className="form-group">
              <label>Password</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                placeholder="••••••••"
              />
            </div>
            <button type="submit" className="btn btn-primary" style={{ width: '100%', marginTop: '10px' }}>
              Sign In
            </button>
          </form>

          <div
            style={{
              marginTop: '24px',
              paddingTop: '16px',
              borderTop: '1px solid var(--border)',
              fontSize: '12px',
              color: 'var(--text-muted)'
            }}
          >
            <strong>Demo Logins:</strong>
            <br />
            Admin: <code>admin@school.com</code> / <code>AdminPassword123!</code>
            <br />
            Teacher: <code>teacher@school.com</code> / <code>TeacherPassword123!</code>
            <br />
            Student: <code>student@school.com</code> / <code>StudentPassword123!</code>
          </div>
        </div>
      </div>
    );
  }

  // ---------------------------------------------------------------------------
  // Authenticated Render (Portal Dashboard)
  // ---------------------------------------------------------------------------
  return (
    <div>
      {/* Top Navbar */}
      <nav className="navbar">
        <div>
          <h3 style={{ fontSize: '18px' }}>School Portal</h3>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <span style={{ fontSize: '14px', color: 'var(--text-muted)' }}>
            {email} <span className={`badge badge-${role}`}>{role}</span>
          </span>
          <button onClick={handleLogout} className="btn btn-danger btn-sm">
            Logout
          </button>
        </div>
      </nav>

      {/* Main Dashboard Layout */}
      <main className="app-container">
        {/* ADMIN VIEW: User Management */}
        {role === 'Admin' && (
          <div className="card">
            <h3>User Management</h3>
            <p style={{ fontSize: '13px', color: 'var(--text-muted)', marginBottom: '16px' }}>
              Registered accounts across the institution
            </p>
            <table className="table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Email</th>
                  <th>Role</th>
                </tr>
              </thead>
              <tbody>
                {users.length === 0 ? (
                  <tr>
                    <td colSpan="3" style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                      No registered users found.
                    </td>
                  </tr>
                ) : (
                  users.map((u) => (
                    <tr key={u.id}>
                      <td>#{u.id}</td>
                      <td>{u.email}</td>
                      <td>
                        <span className={`badge badge-${u.role}`}>{u.role}</span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* TEACHER VIEW: Publish Assignment */}
        {role === 'Teacher' && (
          <div className="card">
            <h3>Publish New Assignment</h3>
            <form onSubmit={handleCreateAssignment} style={{ marginTop: '16px' }}>
              <div className="grid-2">
                <div className="form-group">
                  <label>Title</label>
                  <input
                    type="text"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    required
                    placeholder="e.g. Midterm Essay"
                  />
                </div>
                <div className="form-group">
                  <label>Max Marks</label>
                  <input
                    type="number"
                    value={maxMarks}
                    onChange={(e) => setMaxMarks(e.target.value)}
                    required
                  />
                </div>
              </div>
              <div className="form-group">
                <label>Instructions & Description</label>
                <input
                  type="text"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  required
                  placeholder="Provide clear submission details..."
                />
              </div>
              <button type="submit" className="btn btn-primary">
                Publish Assignment
              </button>
            </form>
          </div>
        )}

        {/* TEACHER/ADMIN VIEW: Submissions Queue */}
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
                  <tr>
                    <td colSpan={role === 'Teacher' ? '6' : '5'} style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                      No submissions found.
                    </td>
                  </tr>
                ) : (
                  submissions.map((s) => (
                    <tr key={s.id}>
                      <td>{s.studentEmail}</td>
                      <td>{s.answerText}</td>
                      <td>
                        <span className={`badge badge-${s.status}`}>{s.status}</span>
                      </td>
                      <td>{s.marks !== null && s.marks !== undefined ? `${s.marks} marks` : 'Unassigned'}</td>
                      <td>{s.feedback || 'No feedback'}</td>
                      {role === 'Teacher' && (
                        <td>
                          <button onClick={() => handleGradeSubmission(s.id)} className="btn btn-primary btn-sm">
                            Grade
                          </button>
                        </td>
                      )}
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* TEACHER/ADMIN VIEW: Active Assignments List */}
        {(role === 'Teacher' || role === 'Admin') && (
          <div className="card">
            <h3>Active Assignments</h3>
            <div style={{ display: 'grid', gap: '16px', marginTop: '16px' }}>
              {assignments.length === 0 ? (
                <p style={{ color: 'var(--text-muted)', fontSize: '14px' }}>No active assignments available.</p>
              ) : (
                assignments.map((a) => (
                  <div
                    key={a.id}
                    style={{
                      border: '1px solid var(--border)',
                      borderRadius: '8px',
                      padding: '16px',
                      background: 'var(--bg-main)'
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                      <div>
                        <h4 style={{ fontSize: '16px' }}>{a.title}</h4>
                        <p style={{ fontSize: '14px', color: 'var(--text-muted)', marginTop: '4px' }}>
                          {a.description}
                        </p>
                      </div>
                      <span className="badge" style={{ background: '#e2e8f0' }}>
                        Max Marks: {a.maxMarks}
                      </span>
                    </div>

                    <div style={{ display: 'flex', gap: '8px', marginTop: '12px' }}>
                      <button onClick={() => handleDeleteAssignment(a.id)} className="btn btn-danger btn-sm">
                        Delete Assignment
                      </button>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>
        )}

        {/* STUDENT VIEW: 2-SEGMENT DASHBOARD */}
        {role === 'Student' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            {/* Segment 1: Pending Assignments */}
            <div className="card">
              <h2 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span>📝</span> Pending Assignments
              </h2>

              {pendingAssignments.length === 0 ? (
                <p style={{ color: 'var(--text-muted)', fontSize: '14px', textAlign: 'center', padding: '16px 0' }}>
                  🎉 All caught up! You have no pending assignments to submit.
                </p>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  {pendingAssignments.map((assignment) => (
                    <div
                      key={assignment.id}
                      style={{
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        padding: '16px',
                        border: '1px solid var(--border)',
                        borderRadius: '8px',
                        background: 'var(--bg-main)'
                      }}
                    >
                      <div>
                        <h3 style={{ fontSize: '16px', fontWeight: '600' }}>{assignment.title}</h3>
                        <p style={{ fontSize: '14px', color: 'var(--text-muted)', marginTop: '4px' }}>{assignment.description}</p>
                        <span className="badge" style={{ background: '#e2e8f0', marginTop: '8px', display: 'inline-block' }}>
                          Max Marks: {assignment.maxMarks}
                        </span>
                      </div>
                      <button
                        onClick={() => handleSubmitAnswer(assignment.id)}
                        className="btn btn-primary btn-sm"
                      >
                        Submit Answer
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Segment 2: Submitted / Done Assignments */}
            <div className="card">
              <h2 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span>✅</span> Completed & Submitted
              </h2>

              {completedSubmissions.length === 0 ? (
                <p style={{ color: 'var(--text-muted)', fontSize: '14px', textAlign: 'center', padding: '16px 0' }}>
                  No completed assignments yet.
                </p>
              ) : (
                <table className="table">
                  <thead>
                    <tr>
                      <th>Assignment</th>
                      <th>Your Answer</th>
                      <th>Status</th>
                      <th>Marks</th>
                      <th>Feedback</th>
                    </tr>
                  </thead>
                  <tbody>
                    {completedSubmissions.map((sub) => (
                      <tr key={sub.id}>
                        <td style={{ fontWeight: '600' }}>{sub.assignmentTitle}</td>
                        <td style={{ color: 'var(--text-muted)', maxWidth: '240px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {sub.answerText}
                        </td>
                        <td>
                          <span className={`badge badge-${sub.status}`}>{sub.status}</span>
                        </td>
                        <td style={{ fontWeight: '500' }}>
                          {sub.marks !== null && sub.marks !== undefined
                            ? `${sub.marks} / ${sub.maxMarks}`
                            : '--'}
                        </td>
                        <td style={{ fontStyle: 'italic', color: 'var(--text-muted)' }}>
                          {sub.feedback || 'Pending review...'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

export default App;