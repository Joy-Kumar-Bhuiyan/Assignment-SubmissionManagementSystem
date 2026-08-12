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
  const [usersList, setUsersList] = useState([]);

  // Academic Management State (Classes & Subjects)
  const [classList, setClassList] = useState([]);
  const [subjectList, setSubjectList] = useState([]);
  const [newClass, setNewClass] = useState({ name: '', section: '' });
  const [newSubject, setNewSubject] = useState({ name: '', code: '' });

  // Allocations & Application Settings State
  const [allocations, setAllocations] = useState([]);
  const [newAllocation, setNewAllocation] = useState({ teacherEmail: '', classId: '', subjectId: '' });
  const [settings, setSettings] = useState({ AcademicYear: '2026-2027', AllowLateSubmissions: 'true' });

  // Form State (Assignment Creation & Editing)
  const [assignmentForm, setAssignmentForm] = useState({
    id: null,
    title: '',
    description: '',
    maxMarks: 100,
    dueDate: '',
    isDraft: false,
    classId: '',
    subjectId: ''
  });

  // Filter State (Submissions per assignment)
  const [selectedAssignmentFilter, setSelectedAssignmentFilter] = useState('ALL');

  // Form Input State (Admin User Creation)
  const [newUser, setNewUser] = useState({ email: '', password: '', role: 'Student', classId: '' });

  // ---------------------------------------------------------------------------
  // Data Fetching
  // ---------------------------------------------------------------------------
  const fetchUsers = useCallback(async () => {
    if (!token || role !== 'Admin') return;
    try {
      const res = await fetch(`${API_BASE_URL}/api/users`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        const data = await res.json();
        setUsersList(data);
      }
    } catch (err) {
      console.error('Error fetching users:', err);
    }
  }, [token, role]);

  const fetchAcademicData = useCallback(async () => {
    if (!token || (role !== 'Admin' && role !== 'Teacher')) return;
    try {
      const [cRes, sRes] = await Promise.all([
        fetch(`${API_BASE_URL}/api/classes`, { headers: { Authorization: `Bearer ${token}` } }),
        fetch(`${API_BASE_URL}/api/subjects`, { headers: { Authorization: `Bearer ${token}` } })
      ]);
      if (cRes.ok) setClassList(await cRes.json());
      if (sRes.ok) setSubjectList(await sRes.json());
    } catch (err) {
      console.error('Error fetching academic data:', err);
    }
  }, [token, role]);

  const fetchAllocationsAndSettings = useCallback(async () => {
    if (!token || role !== 'Admin') return;
    try {
      const [aRes, sRes] = await Promise.all([
        fetch(`${API_BASE_URL}/api/allocations`, { headers: { Authorization: `Bearer ${token}` } }),
        fetch(`${API_BASE_URL}/api/settings`, { headers: { Authorization: `Bearer ${token}` } })
      ]);
      if (aRes.ok) setAllocations(await aRes.json());
      if (sRes.ok) {
        const data = await sRes.json();
        if (Object.keys(data).length > 0) setSettings(data);
      }
    } catch (err) {
      console.error('Error loading system config:', err);
    }
  }, [token, role]);

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
          const altMySubRes = await fetch(`${API_BASE_URL}/api/submissions/my-submissions`, { headers: authHeaders });
          if (altMySubRes.ok) setMySubmissions(await altMySubRes.json());
        }
      }

      // 4. Fetch Academic & Admin Configuration Data
      if (role === 'Admin' || role === 'Teacher') {
        await fetchAcademicData();
      }
      if (role === 'Admin') {
        await fetchUsers();
        await fetchAllocationsAndSettings();
      }
    } catch (error) {
      console.error('Error fetching portal data:', error);
    }
  }, [token, role, fetchUsers, fetchAcademicData, fetchAllocationsAndSettings]);

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
    setUsersList([]);
    setClassList([]);
    setSubjectList([]);
    setAllocations([]);
    setSettings({ AcademicYear: '2026-2027', AllowLateSubmissions: 'true' });
    localStorage.clear();
  };

  // ---------------------------------------------------------------------------
  // Admin Handlers (Users, Classes, Subjects, Allocations, Settings)
  // ---------------------------------------------------------------------------
  const handleCreateUser = async (e) => {
    e.preventDefault();
    try {
      const payload = {
        email: newUser.email,
        password: newUser.password,
        role: newUser.role,
        classId: newUser.role === 'Student' && newUser.classId ? Number(newUser.classId) : null
      };

      const res = await fetch(`${API_BASE_URL}/api/users`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(payload)
      });

      if (res.ok) {
        alert('User created successfully!');
        setNewUser({ email: '', password: '', role: 'Student', classId: '' });
        fetchUsers();
      } else {
        const errData = await res.json();
        alert(errData.message || 'Failed to create user.');
      }
    } catch (err) {
      console.error('Error creating user:', err);
    }
  };

  const handleDeleteUser = async (userId) => {
    if (!window.confirm('Are you sure you want to delete this user?')) return;
    try {
      const res = await fetch(`${API_BASE_URL}/api/users/${userId}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      });
      if (res.ok) {
        alert('User deleted.');
        fetchUsers();
      } else {
        alert('Failed to delete user.');
      }
    } catch (err) {
      console.error('Error deleting user:', err);
    }
  };

  const handleCreateClass = async (e) => {
    e.preventDefault();
    try {
      const res = await fetch(`${API_BASE_URL}/api/classes`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(newClass)
      });
      if (res.ok) {
        alert('Class added successfully!');
        setNewClass({ name: '', section: '' });
        fetchAcademicData();
      } else {
        alert('Failed to add class.');
      }
    } catch (err) {
      console.error('Error creating class:', err);
    }
  };

  const handleCreateSubject = async (e) => {
    e.preventDefault();
    try {
      const res = await fetch(`${API_BASE_URL}/api/subjects`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify(newSubject)
      });
      if (res.ok) {
        alert('Subject added successfully!');
        setNewSubject({ name: '', code: '' });
        fetchAcademicData();
      } else {
        alert('Failed to add subject.');
      }
    } catch (err) {
      console.error('Error creating subject:', err);
    }
  };

  const handleCreateAllocation = async (e) => {
    e.preventDefault();
    try {
      const res = await fetch(`${API_BASE_URL}/api/allocations`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          teacherEmail: newAllocation.teacherEmail,
          classId: Number(newAllocation.classId),
          subjectId: Number(newAllocation.subjectId)
        })
      });
      if (res.ok) {
        alert('Teacher allocated successfully!');
        setNewAllocation({ teacherEmail: '', classId: '', subjectId: '' });
        fetchAllocationsAndSettings();
      } else {
        alert('Failed to create allocation.');
      }
    } catch (err) {
      console.error('Error creating allocation:', err);
    }
  };

  const handleSaveSetting = async (key, value) => {
    try {
      const res = await fetch(`${API_BASE_URL}/api/settings`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ key, value })
      });
      if (res.ok) {
        alert(`${key} updated!`);
      } else {
        alert(`Failed to update ${key}.`);
      }
    } catch (err) {
      console.error('Error saving setting:', err);
    }
  };

  // ---------------------------------------------------------------------------
  // Assignment Save / Edit Actions
  // ---------------------------------------------------------------------------
  const handleSaveAssignment = async (e) => {
    e.preventDefault();
    const isEditing = Boolean(assignmentForm.id);
    const url = isEditing
      ? `${API_BASE_URL}/api/assignments/${assignmentForm.id}`
      : `${API_BASE_URL}/api/assignments`;
    const method = isEditing ? 'PUT' : 'POST';

    const payload = {
      title: assignmentForm.title,
      description: assignmentForm.description,
      maxMarks: Number(assignmentForm.maxMarks),
      dueDate: assignmentForm.dueDate ? new Date(assignmentForm.dueDate).toISOString() : null,
      isDraft: assignmentForm.isDraft,
      classId: assignmentForm.classId ? Number(assignmentForm.classId) : null,
      subjectId: assignmentForm.subjectId ? Number(assignmentForm.subjectId) : null,
    };

    try {
      const res = await fetch(url, {
        method,
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      });

      if (res.ok) {
        alert(`Assignment ${isEditing ? 'updated' : 'created'} successfully!`);
        setAssignmentForm({
          id: null,
          title: '',
          description: '',
          maxMarks: 100,
          dueDate: '',
          isDraft: false,
          classId: '',
          subjectId: '',
        });
        fetchData();
      } else {
        alert(`Failed to ${isEditing ? 'update' : 'create'} assignment.`);
      }
    } catch (err) {
      console.error('Error saving assignment:', err);
      alert('Network error while saving assignment.');
    }
  };

  const handleEditClick = (assignment) => {
    setAssignmentForm({
      id: assignment.id,
      title: assignment.title || '',
      description: assignment.description || '',
      maxMarks: assignment.maxMarks || 100,
      dueDate: assignment.dueDate ? assignment.dueDate.slice(0, 16) : '',
      isDraft: Boolean(assignment.isDraft),
      classId: assignment.classId || '',
      subjectId: assignment.subjectId || '',
    });
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
        const err = await response.json();
        alert(err.message || 'Failed to submit answer.');
      }
    } catch (error) {
      console.error('Error submitting answer:', error);
      alert('Network error while submitting answer.');
    }
  };

  // Alias for modal submission handler compatibility
  const handleOpenSubmitModal = (assignmentId) => {
    handleSubmitAnswer(assignmentId);
  };

  const handleGradeSubmission = async (subId) => {
    const marks = prompt('Enter Marks:');
    if (marks === null) return;

    const feedback = prompt('Enter Teacher Feedback:');
    if (feedback === null) return;

    const status = prompt('Enter Status (e.g. Graded, Returned for Revision, Rejected):', 'Graded');

    try {
      const response = await fetch(`${API_BASE_URL}/api/submissions/${subId}/grade`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({ 
          marks: marks !== '' ? parseInt(marks, 10) : null, 
          feedback,
          status: status || 'Graded'
        })
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

  // Helper to resolve Class Name & Subject Name for Assignment cards
  const getAssignmentTags = (a) => {
    const className =
      a.className ||
      a.class?.name ||
      (a.classId ? classList.find((c) => c.id === a.classId)?.name : null);

    const section =
      a.classSection ||
      a.class?.section ||
      (a.classId ? classList.find((c) => c.id === a.classId)?.section : null);

    const subjectName =
      a.subjectName ||
      a.subject?.name ||
      (a.subjectId ? subjectList.find((s) => s.id === a.subjectId)?.name : null);

    return {
      className: className ? `${className}${section ? ` (${section})` : ''}` : 'General',
      subjectName: subjectName || 'General'
    };
  };

  // Filter Submissions for Teacher/Admin View
  const filteredSubmissions = submissions.filter((s) => {
    if (selectedAssignmentFilter === 'ALL') return true;
    return String(s.assignmentId) === String(selectedAssignmentFilter);
  });

  // ---------------------------------------------------------------------------
  // Student Computations
  // ---------------------------------------------------------------------------
  const submittedAssignmentIds = new Set(
    mySubmissions.map((sub) => sub.assignmentId)
  );

  const pendingAssignments = assignments
    .filter((a) => !a.isDraft && !submittedAssignmentIds.has(a.id))
    .map((a) => {
      const tags = getAssignmentTags(a);
      return {
        ...a,
        className: tags.className,
        subjectName: tags.subjectName
      };
    });

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
        {/* ADMIN VIEW */}
        {role === 'Admin' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '24px', marginBottom: '24px' }}>
            {/* Create User Form */}
            <div className="card">
              <h2 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px' }}>
                Admin Panel: Create New User
              </h2>
              <form onSubmit={handleCreateUser} style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '12px' }}>
                <input
                  type="email"
                  placeholder="Email address"
                  value={newUser.email}
                  onChange={(e) => setNewUser({ ...newUser, email: e.target.value })}
                  required
                  style={{ padding: '8px', border: '1px solid var(--border)', borderRadius: '4px' }}
                />
                <input
                  type="password"
                  placeholder="Password"
                  value={newUser.password}
                  onChange={(e) => setNewUser({ ...newUser, password: e.target.value })}
                  required
                  style={{ padding: '8px', border: '1px solid var(--border)', borderRadius: '4px' }}
                />
                <select
                  value={newUser.role}
                  onChange={(e) => setNewUser({ ...newUser, role: e.target.value })}
                  style={{ padding: '8px', border: '1px solid var(--border)', borderRadius: '4px' }}
                >
                  <option value="Student">Student</option>
                  <option value="Teacher">Teacher</option>
                  <option value="Admin">Admin</option>
                </select>

                {newUser.role === 'Student' && (
                  <select
                    value={newUser.classId}
                    onChange={(e) => setNewUser({ ...newUser, classId: e.target.value })}
                    style={{ padding: '8px', border: '1px solid var(--border)', borderRadius: '4px' }}
                  >
                    <option value="">Select Enrolled Class</option>
                    {classList.map((c) => (
                      <option key={c.id} value={c.id}>{c.name} - Section {c.section}</option>
                    ))}
                  </select>
                )}

                <button type="submit" className="btn btn-primary">
                  Add User
                </button>
              </form>
            </div>

            {/* Users List Table */}
            <div className="card">
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
                <h2 style={{ fontSize: '18px', fontWeight: 'bold' }}>System Users</h2>
                <button onClick={fetchUsers} className="btn btn-sm" style={{ background: '#e2e8f0' }}>
                  Refresh List
                </button>
              </div>

              <table className="table">
                <thead>
                  <tr>
                    <th>ID</th>
                    <th>Email</th>
                    <th>Role</th>
                    <th>Enrolled Class</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {usersList.length === 0 ? (
                    <tr>
                      <td colSpan="5" style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                        No users found.
                      </td>
                    </tr>
                  ) : (
                    usersList.map((u) => {
                      const userClass = classList.find((c) => c.id === u.classId);
                      return (
                        <tr key={u.id}>
                          <td>#{u.id}</td>
                          <td style={{ fontWeight: '500' }}>{u.email}</td>
                          <td>
                            <span className={`badge badge-${u.role}`}>{u.role}</span>
                          </td>
                          <td>
                            {userClass ? `${userClass.name} (${userClass.section})` : '--'}
                          </td>
                          <td>
                            <button
                              onClick={() => handleDeleteUser(u.id)}
                              className="btn btn-danger btn-sm"
                            >
                              Delete
                            </button>
                          </td>
                        </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>

            {/* ACADEMIC MANAGEMENT (CLASSES & SUBJECTS) */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '24px' }}>
              {/* Manage Classes */}
              <div className="card">
                <h3 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px' }}>Classes Management</h3>
                <form onSubmit={handleCreateClass} style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
                  <input
                    type="text"
                    placeholder="Class Name (e.g. Class 10)"
                    value={newClass.name}
                    onChange={(e) => setNewClass({ ...newClass, name: e.target.value })}
                    required
                    style={{ flex: 1, padding: '8px', border: '1px solid var(--border)', borderRadius: '4px', fontSize: '14px' }}
                  />
                  <input
                    type="text"
                    placeholder="Section (e.g. A)"
                    value={newClass.section}
                    onChange={(e) => setNewClass({ ...newClass, section: e.target.value })}
                    required
                    style={{ width: '90px', padding: '8px', border: '1px solid var(--border)', borderRadius: '4px', fontSize: '14px' }}
                  />
                  <button type="submit" className="btn btn-primary btn-sm">
                    Add
                  </button>
                </form>
                <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                  {classList.length === 0 ? (
                    <li style={{ color: 'var(--text-muted)', fontSize: '14px' }}>No classes created yet.</li>
                  ) : (
                    classList.map((c) => (
                      <li key={c.id} style={{ padding: '8px 0', borderBottom: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between', fontSize: '14px' }}>
                        <span><strong>{c.name}</strong> - Section {c.section}</span>
                      </li>
                    ))
                  )}
                </ul>
              </div>

              {/* Manage Subjects */}
              <div className="card">
                <h3 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px' }}>Subjects Management</h3>
                <form onSubmit={handleCreateSubject} style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
                  <input
                    type="text"
                    placeholder="Subject Name (e.g. Physics)"
                    value={newSubject.name}
                    onChange={(e) => setNewSubject({ ...newSubject, name: e.target.value })}
                    required
                    style={{ flex: 1, padding: '8px', border: '1px solid var(--border)', borderRadius: '4px', fontSize: '14px' }}
                  />
                  <input
                    type="text"
                    placeholder="Code (e.g. PHY101)"
                    value={newSubject.code}
                    onChange={(e) => setNewSubject({ ...newSubject, code: e.target.value })}
                    required
                    style={{ width: '110px', padding: '8px', border: '1px solid var(--border)', borderRadius: '4px', fontSize: '14px' }}
                  />
                  <button type="submit" className="btn btn-primary btn-sm">
                    Add
                  </button>
                </form>
                <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
                  {subjectList.length === 0 ? (
                    <li style={{ color: 'var(--text-muted)', fontSize: '14px' }}>No subjects created yet.</li>
                  ) : (
                    subjectList.map((s) => (
                      <li key={s.id} style={{ padding: '8px 0', borderBottom: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between', fontSize: '14px' }}>
                        <span><strong>{s.name}</strong> ({s.code})</span>
                      </li>
                    ))
                  )}
                </ul>
              </div>
            </div>

            {/* TEACHER ALLOCATION & SYSTEM CONFIGURATION */}
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '24px' }}>
              {/* Assign Teachers to Class & Subject */}
              <div className="card">
                <h3 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px' }}>Assign Teacher to Subject & Class</h3>
                <form onSubmit={handleCreateAllocation} style={{ display: 'flex', flexDirection: 'column', gap: '12px', marginBottom: '16px' }}>
                  <select
                    style={{ padding: '8px', border: '1px solid var(--border)', borderRadius: '4px', fontSize: '14px', width: '100%' }}
                    value={newAllocation.teacherEmail}
                    onChange={(e) => setNewAllocation({ ...newAllocation, teacherEmail: e.target.value })}
                    required
                  >
                    <option value="">Select Teacher</option>
                    {usersList.filter((u) => u.role === 'Teacher').map((t) => (
                      <option key={t.id} value={t.email}>{t.email}</option>
                    ))}
                  </select>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                    <select
                      style={{ padding: '8px', border: '1px solid var(--border)', borderRadius: '4px', fontSize: '14px', width: '100%' }}
                      value={newAllocation.classId}
                      onChange={(e) => setNewAllocation({ ...newAllocation, classId: e.target.value })}
                      required
                    >
                      <option value="">Select Class</option>
                      {classList.map((c) => (
                        <option key={c.id} value={c.id}>{c.name} - {c.section}</option>
                      ))}
                    </select>

                    <select
                      style={{ padding: '8px', border: '1px solid var(--border)', borderRadius: '4px', fontSize: '14px', width: '100%' }}
                      value={newAllocation.subjectId}
                      onChange={(e) => setNewAllocation({ ...newAllocation, subjectId: e.target.value })}
                      required
                    >
                      <option value="">Select Subject</option>
                      {subjectList.map((s) => (
                        <option key={s.id} value={s.id}>{s.name}</option>
                      ))}
                    </select>
                  </div>

                  <button type="submit" className="btn btn-primary" style={{ width: '100%', fontSize: '14px' }}>
                    Assign Teacher
                  </button>
                </form>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {allocations.length === 0 ? (
                    <p style={{ color: 'var(--text-muted)', fontSize: '14px' }}>No allocations created yet.</p>
                  ) : (
                    allocations.map((a) => (
                      <div key={a.id} style={{ padding: '8px 0', borderBottom: '1px solid var(--border)' }}>
                        <p style={{ fontWeight: '600', fontSize: '14px' }}>{a.teacherEmail}</p>
                        <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{a.className} • {a.subjectName}</p>
                      </div>
                    ))
                  )}
                </div>
              </div>

              {/* Application Level Settings */}
              <div className="card">
                <h3 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px' }}>Application Settings</h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', fontSize: '14px' }}>
                  <div>
                    <label style={{ display: 'block', fontWeight: '500', marginBottom: '4px' }}>Academic Year</label>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      <input
                        type="text"
                        style={{ flex: 1, padding: '8px', border: '1px solid var(--border)', borderRadius: '4px' }}
                        value={settings.AcademicYear || ''}
                        onChange={(e) => setSettings({ ...settings, AcademicYear: e.target.value })}
                      />
                      <button
                        type="button"
                        onClick={() => handleSaveSetting('AcademicYear', settings.AcademicYear)}
                        className="btn btn-sm"
                        style={{ background: '#334155', color: '#fff' }}
                      >
                        Save
                      </button>
                    </div>
                  </div>

                  <div>
                    <label style={{ display: 'block', fontWeight: '500', marginBottom: '4px' }}>Allow Late Submissions</label>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      <select
                        style={{ flex: 1, padding: '8px', border: '1px solid var(--border)', borderRadius: '4px' }}
                        value={settings.AllowLateSubmissions || 'true'}
                        onChange={(e) => setSettings({ ...settings, AllowLateSubmissions: e.target.value })}
                      >
                        <option value="true">Enabled</option>
                        <option value="false">Disabled</option>
                      </select>
                      <button
                        type="button"
                        onClick={() => handleSaveSetting('AllowLateSubmissions', settings.AllowLateSubmissions)}
                        className="btn btn-sm"
                        style={{ background: '#334155', color: '#fff' }}
                      >
                        Save
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* TEACHER & ADMIN VIEW: CREATE / EDIT ASSIGNMENT */}
        {(role === 'Teacher' || role === 'Admin') && (
          <div className="card" style={{ marginBottom: '24px' }}>
            <h2 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px' }}>
              {assignmentForm.id ? '✏️ Edit Assignment' : '➕ Create Assignment'}
            </h2>
            <form onSubmit={handleSaveAssignment} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px' }}>
                <input
                  type="text"
                  placeholder="Title"
                  value={assignmentForm.title}
                  onChange={(e) => setAssignmentForm({ ...assignmentForm, title: e.target.value })}
                  required
                  style={{ padding: '8px 12px', border: '1px solid var(--border)', borderRadius: '6px', fontSize: '14px' }}
                />
                <input
                  type="number"
                  placeholder="Max Marks"
                  value={assignmentForm.maxMarks}
                  onChange={(e) => setAssignmentForm({ ...assignmentForm, maxMarks: e.target.value })}
                  required
                  style={{ padding: '8px 12px', border: '1px solid var(--border)', borderRadius: '6px', fontSize: '14px' }}
                />
                <input
                  type="datetime-local"
                  value={assignmentForm.dueDate}
                  onChange={(e) => setAssignmentForm({ ...assignmentForm, dueDate: e.target.value })}
                  style={{ padding: '8px 12px', border: '1px solid var(--border)', borderRadius: '6px', fontSize: '14px' }}
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px' }}>
                <select
                  value={assignmentForm.classId}
                  onChange={(e) => setAssignmentForm({ ...assignmentForm, classId: e.target.value })}
                  style={{ padding: '8px 12px', border: '1px solid var(--border)', borderRadius: '6px', fontSize: '14px' }}
                >
                  <option value="">Select Target Class (Optional)</option>
                  {classList.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name} - Section {c.section}
                    </option>
                  ))}
                </select>

                <select
                  value={assignmentForm.subjectId}
                  onChange={(e) => setAssignmentForm({ ...assignmentForm, subjectId: e.target.value })}
                  style={{ padding: '8px 12px', border: '1px solid var(--border)', borderRadius: '6px', fontSize: '14px' }}
                >
                  <option value="">Select Subject (Optional)</option>
                  {subjectList.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.code})
                    </option>
                  ))}
                </select>
              </div>

              <textarea
                placeholder="Instructions / Description..."
                rows={3}
                value={assignmentForm.description}
                onChange={(e) => setAssignmentForm({ ...assignmentForm, description: e.target.value })}
                style={{ padding: '8px 12px', border: '1px solid var(--border)', borderRadius: '6px', fontSize: '14px', resize: 'vertical' }}
              />

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '14px', cursor: 'pointer' }}>
                  <input
                    type="checkbox"
                    checked={assignmentForm.isDraft}
                    onChange={(e) => setAssignmentForm({ ...assignmentForm, isDraft: e.target.checked })}
                  />
                  Save as Draft (Hidden from students)
                </label>

                <div style={{ display: 'flex', gap: '8px' }}>
                  {assignmentForm.id && (
                    <button
                      type="button"
                      onClick={() =>
                        setAssignmentForm({
                          id: null,
                          title: '',
                          description: '',
                          maxMarks: 100,
                          dueDate: '',
                          isDraft: false,
                          classId: '',
                          subjectId: ''
                        })
                      }
                      className="btn btn-sm"
                      style={{ background: '#e2e8f0', color: '#334155' }}
                    >
                      Cancel Edit
                    </button>
                  )}
                  <button type="submit" className="btn btn-primary btn-sm">
                    {assignmentForm.id
                      ? 'Update Assignment'
                      : assignmentForm.isDraft
                      ? 'Save Draft'
                      : 'Publish Assignment'}
                  </button>
                </div>
              </div>
            </form>
          </div>
        )}

        {/* TEACHER/ADMIN VIEW: SUBMISSIONS QUEUE & FILTER */}
        {(role === 'Teacher' || role === 'Admin') && (
          <div className="card" style={{ marginBottom: '24px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '12px' }}>
              <h3 style={{ fontSize: '18px', fontWeight: 'bold' }}>Submissions Queue</h3>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                <label style={{ fontWeight: '600', fontSize: '14px', color: 'var(--text-muted)' }}>
                  Filter by Assignment:
                </label>
                <select
                  style={{ padding: '6px 12px', border: '1px solid var(--border)', borderRadius: '6px', fontSize: '14px' }}
                  value={selectedAssignmentFilter}
                  onChange={(e) => setSelectedAssignmentFilter(e.target.value)}
                >
                  <option value="ALL">All Assignments</option>
                  {assignments.map((a) => (
                    <option key={a.id} value={a.id}>
                      {a.title}
                    </option>
                  ))}
                </select>
              </div>
            </div>

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
                {filteredSubmissions.length === 0 ? (
                  <tr>
                    <td colSpan={role === 'Teacher' ? '6' : '5'} style={{ textAlign: 'center', color: 'var(--text-muted)' }}>
                      No submissions found.
                    </td>
                  </tr>
                ) : (
                  filteredSubmissions.map((s) => (
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

        {/* TEACHER/ADMIN VIEW: ACTIVE ASSIGNMENTS LIST */}
        {(role === 'Teacher' || role === 'Admin') && (
          <div className="card" style={{ marginBottom: '24px' }}>
            <h3>Active Assignments</h3>
            <div style={{ display: 'grid', gap: '16px', marginTop: '16px' }}>
              {assignments.length === 0 ? (
                <p style={{ color: 'var(--text-muted)', fontSize: '14px' }}>No active assignments available.</p>
              ) : (
                assignments.map((a) => {
                  const tags = getAssignmentTags(a);
                  return (
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
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                            <h4 style={{ fontSize: '16px' }}>{a.title}</h4>
                            {a.isDraft && (
                              <span style={{ background: '#fef3c7', color: '#92400e', fontSize: '12px', fontWeight: 'bold', padding: '2px 8px', borderRadius: '4px' }}>
                                DRAFT
                              </span>
                            )}
                          </div>
                          <p style={{ fontSize: '14px', color: 'var(--text-muted)', marginTop: '4px' }}>
                            {a.description}
                          </p>

                          {/* Class & Subject Tags */}
                          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '8px', flexWrap: 'wrap' }}>
                            {tags.className && (
                              <span className="badge" style={{ background: '#e0e7ff', color: '#3730a3', fontWeight: '600', padding: '2px 8px', fontSize: '12px', borderRadius: '4px' }}>
                                📚 {tags.className}
                              </span>
                            )}
                            {tags.subjectName && (
                              <span className="badge" style={{ background: '#d1fae5', color: '#065f46', fontWeight: '600', padding: '2px 8px', fontSize: '12px', borderRadius: '4px' }}>
                                📖 {tags.subjectName}
                              </span>
                            )}
                            {a.dueDate && (
                              <span style={{ fontSize: '12px', color: '#dc2626', fontWeight: '500' }}>
                                ⏰ Due: {new Date(a.dueDate).toLocaleString()}
                              </span>
                            )}
                          </div>
                        </div>

                        <span className="badge" style={{ background: '#e2e8f0' }}>
                          Max Marks: {a.maxMarks}
                        </span>
                      </div>

                      <div style={{ display: 'flex', gap: '8px', marginTop: '12px' }}>
                        <button onClick={() => handleEditClick(a)} className="btn btn-sm" style={{ background: '#2563eb', color: '#fff' }}>
                          Edit
                        </button>
                        <button onClick={() => handleDeleteAssignment(a.id)} className="btn btn-danger btn-sm">
                          Delete
                        </button>
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </div>
        )}

        {/* STUDENT VIEW: 2-SEGMENT DASHBOARD (ENHANCED) */}
        {role === 'Student' && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
            {/* Segment 1: Pending Class Assignments */}
            <div className="card">
              <h2 style={{ fontSize: '18px', fontWeight: 'bold', marginBottom: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}>
                <span>📝</span> Class Assignments
              </h2>

              {pendingAssignments.length === 0 ? (
                <p style={{ color: 'var(--text-muted)', fontSize: '14px', textAlign: 'center', padding: '16px 0' }}>
                  🎉 All caught up! No pending assignments for your class.
                </p>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                  {pendingAssignments.map((assignment) => {
                    const isOverdue = assignment.dueDate && new Date() > new Date(assignment.dueDate);
                    return (
                      <div
                        key={assignment.id}
                        style={{
                          padding: '16px',
                          border: '1px solid var(--border)',
                          borderRadius: '8px',
                          background: 'var(--bg-main)'
                        }}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '8px' }}>
                          <div>
                            <h3 style={{ fontSize: '16px', fontWeight: '600' }}>{assignment.title}</h3>
                            <div style={{ display: 'flex', gap: '8px', margin: '4px 0' }}>
                              <span className="badge" style={{ background: '#e0e7ff', color: '#3730a3', fontWeight: '600', padding: '2px 8px', fontSize: '12px', borderRadius: '4px' }}>
                                📚 {assignment.className}
                              </span>
                              <span className="badge" style={{ background: '#d1fae5', color: '#065f46', fontWeight: '600', padding: '2px 8px', fontSize: '12px', borderRadius: '4px' }}>
                                📖 {assignment.subjectName}
                              </span>
                            </div>
                          </div>
                          <span className="badge" style={{ background: '#e2e8f0' }}>
                            Max Marks: {assignment.maxMarks}
                          </span>
                        </div>

                        <p style={{ fontSize: '14px', color: 'var(--text-muted)', marginBottom: '12px' }}>{assignment.description}</p>

                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingTop: '8px', borderTop: '1px solid var(--border)', fontSize: '12px' }}>
                          {assignment.dueDate ? (
                            <span style={{ fontWeight: '600', color: isOverdue ? '#dc2626' : 'var(--text-muted)' }}>
                              ⏰ Deadline: {new Date(assignment.dueDate).toLocaleString()}
                              {isOverdue && ' (OVERDUE)'}
                            </span>
                          ) : (
                            <span style={{ color: 'var(--text-muted)' }}>No deadline set</span>
                          )}

                          <button
                            onClick={() => handleOpenSubmitModal(assignment.id)}
                            className={`btn btn-sm ${isOverdue ? 'btn-danger' : 'btn-primary'}`}
                          >
                            Submit Answer
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>

            {/* Segment 2: Completed & Resubmission Support */}
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
                      <th>Submitted At</th>
                      <th>Status</th>
                      <th>Marks</th>
                      <th>Feedback</th>
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {completedSubmissions.map((sub) => (
                      <tr key={sub.id}>
                        <td style={{ fontWeight: '600' }}>{sub.assignmentTitle}</td>
                        <td style={{ color: 'var(--text-muted)', maxWidth: '200px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {sub.answerText}
                        </td>
                        <td style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                          {sub.submittedAt ? new Date(sub.submittedAt).toLocaleString() : '--'}
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
                        <td>
                          <button
                            onClick={() => handleOpenSubmitModal(sub.assignmentId)}
                            className="btn btn-sm"
                            style={{ background: '#e2e8f0', color: '#1e293b', fontSize: '12px' }}
                          >
                            Update / Resubmit
                          </button>
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
