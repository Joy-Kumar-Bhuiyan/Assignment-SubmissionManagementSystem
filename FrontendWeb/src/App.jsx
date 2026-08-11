import { useState, useEffect } from 'react';

function App() {
  const [token, setToken] = useState(localStorage.getItem('token') || '');
  const [role, setRole] = useState(localStorage.getItem('role') || '');
  const [email, setEmail] = useState(localStorage.getItem('email') || '');
  const [password, setPassword] = useState('');

  // Data States
  const [assignments, setAssignments] = useState([]);
  const [submissions, setSubmissions] = useState([]);
  const [mySubmissions, setMySubmissions] = useState([]);
  const [users, setUsers] = useState([]);

  // Form States
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

    // Fetch Assignments
    const assignRes = await fetch('http://localhost:5000/api/assignments', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (assignRes.ok) setAssignments(await assignRes.json());

    // Role-Specific Data Fetching
    if (role === 'Teacher' || role === 'Admin') {
      const subRes = await fetch('http://localhost:5000/api/submissions', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (subRes.ok) setSubmissions(await subRes.json());
    }

    if (role === 'Student') {
      const mySubRes = await fetch('http://localhost:5000/api/submissions/my', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      if (mySubRes.ok) setMySubmissions(await mySubRes.json());
    }

    if (role === 'Admin') {
      const userRes = await fetch('http://localhost:5000/api/admin/users', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
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
    const answer = prompt("Enter your assignment submission text:");
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
      <div style={{ maxWidth: '400px', margin: '50px auto', fontFamily: 'Arial', padding: '20px', border: '1px solid #ccc', borderRadius: '8px' }}>
        <h2>School System Login</h2>
        <p style={{ fontSize: '12px', color: '#666' }}>
          <strong>Demo Logins:</strong><br/>
          Admin: admin@school.com / AdminPassword123!<br/>
          Teacher: teacher@school.com / TeacherPassword123!<br/>
          Student: student@school.com / StudentPassword123!
        </p>
        <form onSubmit={handleLogin}>
          <div style={{ marginBottom: '10px' }}>
            <label>Email:</label><br/>
            <input type="email" value={email} onChange={e => setEmail(e.target.value)} required style={{ width: '100%', padding: '8px' }} />
          </div>
          <div style={{ marginBottom: '15px' }}>
            <label>Password:</label><br/>
            <input type="password" value={password} onChange={e => setPassword(e.target.value)} required style={{ width: '100%', padding: '8px' }} />
          </div>
          <button type="submit" style={{ width: '100%', padding: '10px', backgroundColor: '#007bff', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Login</button>
        </form>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '900px', margin: '20px auto', fontFamily: 'Arial', padding: '20px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '2px solid #ddd', paddingBottom: '10px' }}>
        <div>
          <h2>Management Portal</h2>
          <span>Logged in as: <strong>{email}</strong> ({role})</span>
        </div>
        <button onClick={handleLogout} style={{ padding: '8px 16px', backgroundColor: '#dc3545', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Logout</button>
      </div>

      {/* ADMIN VIEW */}
      {role === 'Admin' && (
        <div style={{ marginTop: '20px', background: '#f8f9fa', padding: '15px', borderRadius: '5px' }}>
          <h3>Admin Panel: User Accounts</h3>
          <ul>
            {users.map(u => <li key={u.id}>{u.email} - <strong>{u.role}</strong></li>)}
          </ul>
        </div>
      )}

      {/* TEACHER VIEW */}
      {role === 'Teacher' && (
        <div style={{ marginTop: '20px' }}>
          <form onSubmit={handleCreateAssignment} style={{ background: '#f8f9fa', padding: '15px', borderRadius: '5px', marginBottom: '20px' }}>
            <h3>Create New Assignment</h3>
            <input type="text" placeholder="Title" value={title} onChange={e => setTitle(e.target.value)} required style={{ padding: '8px', marginRight: '10px' }} />
            <input type="text" placeholder="Description" value={description} onChange={e => setDescription(e.target.value)} required style={{ padding: '8px', marginRight: '10px' }} />
            <input type="number" placeholder="Max Marks" value={maxMarks} onChange={e => setMaxMarks(e.target.value)} required style={{ padding: '8px', marginRight: '10px', width: '80px' }} />
            <button type="submit" style={{ padding: '8px 16px', backgroundColor: '#28a745', color: 'white', border: 'none', cursor: 'pointer' }}>Publish</button>
          </form>

          <h3>Student Submissions to Grade</h3>
          {submissions.length === 0 ? <p>No submissions yet.</p> : (
            <table border="1" cellPadding="10" style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr>
                  <th>Student</th>
                  <th>Answer</th>
                  <th>Status</th>
                  <th>Marks</th>
                  <th>Feedback</th>
                  <th>Action</th>
                </tr>
              </thead>
              <tbody>
                {submissions.map(s => (
                  <tr key={s.id}>
                    <td>{s.studentEmail}</td>
                    <td>{s.answerText}</td>
                    <td>{s.status}</td>
                    <td>{s.marks ?? 'N/A'}</td>
                    <td>{s.feedback ?? 'N/A'}</td>
                    <td>
                      <button onClick={() => handleGradeSubmission(s.id)}>Grade</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* ASSIGNMENTS VIEW */}
      <h3 style={{ marginTop: '30px' }}>Assignments</h3>
      {assignments.map(a => (
        <div key={a.id} style={{ border: '1px solid #ddd', padding: '15px', marginBottom: '10px', borderRadius: '5px' }}>
          <h4>{a.title} (Max Marks: {a.maxMarks})</h4>
          <p>{a.description}</p>
          {role === 'Student' && (
            <button onClick={() => handleSubmitAnswer(a.id)} style={{ padding: '6px 12px', backgroundColor: '#007bff', color: 'white', border: 'none', cursor: 'pointer' }}>Submit Answer</button>
          )}
        </div>
      ))}

      {/* STUDENT SUBMISSION STATUS */}
      {role === 'Student' && (
        <div style={{ marginTop: '30px' }}>
          <h3>My Submission Status & Feedback</h3>
          {mySubmissions.length === 0 ? <p>You have not submitted any assignments.</p> : (
            <ul>
              {mySubmissions.map(s => (
                <li key={s.id} style={{ marginBottom: '10px' }}>
                  Submission ID #{s.id} - <strong>Status: {s.status}</strong><br/>
                  Marks: {s.marks ?? 'Pending'} | Feedback: {s.feedback ?? 'None'}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}

export default App;