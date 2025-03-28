import React, { useContext, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import UpdateProfile from '../stranice/UpdateProfile.js';
import { AppContext } from '../App.js';

export default function Header() {
  const [showDropdown, setShowDropdown] = useState(false);
  const [showLogoutConfirmation, setShowLogoutConfirmation] = useState(false);
  const [showProfileUpdate, setShowProfileUpdate] = useState(false);
  const [showDeleteConfirmation, setShowDeleteConfirmation] = useState(false);
  const { setNaProfilu } = useContext(AppContext);

  const navigate = useNavigate();

  const handleLogout = () => {
    fetch('http://localhost:5018/api/Korisnik/izlogujKorisnika', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${sessionStorage.getItem('jwt')}`
      },
      credentials: 'include',

    })
      .then((response) => {
        if (response.ok) {
          sessionStorage.removeItem('jwt');
          navigate('/');
          setNaProfilu(false);
        } else {
          console.error('Logout failed');
        }
      })
      .catch((error) => console.error('Error:', error));
  };
  const handleDelete = () => {
    fetch('http://localhost:5018/api/Korisnik/obrisiProfil', {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${sessionStorage.getItem('jwt')}`
      },
      credentials: 'include',

    })
      .then((response) => {
        if (response.ok) {
          sessionStorage.removeItem('jwt');
          navigate('/');
          setNaProfilu(false);
            //dodati i za korisnika da je null
        } else {
          console.error('Delete failed');
        }
      })
      .catch((error) => console.error('Error:', error));
  };

  return (
    <header className="bg-green-700 text-white flex justify-between items-center px-4 py-2">
      <div className="text-xl font-bold">Foodie</div>

      <div className="flex items-center gap-4">
        <button
          onClick={() => navigate('/korpa')}
          className="text-white text-xl"
        >
          🛒
        </button>
        <button
          onClick={() => setShowDropdown(!showDropdown)}
          className="text-white text-xl"
        >
          ⋮
        </button>

        {showDropdown && (
          <div className="bg-white rounded-lg shadow-lg z-30 absolute top-10 right-0 w-48">
            <button
              className="block w-full text-left px-4 py-2 text-gray-700 hover:bg-gray-100"
              onClick={() => setShowLogoutConfirmation(true)}
            >
              Log Out
            </button>
            <button
              className="block w-full text-left px-4 py-2 text-gray-700 hover:bg-gray-100"
              onClick={() => setShowProfileUpdate(true)}
            >
              Update Profile
            </button>
            <button
              className="block w-full text-left px-4 py-2 text-gray-700 hover:bg-gray-100"
              onClick={() => setShowDeleteConfirmation(true)}
            >
              Delete Profile
            </button>
          </div>
        )}
      </div>
      {showLogoutConfirmation && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
          <div className="bg-white rounded-lg shadow-lg p-6">
            <p className="text-center text-gray-800 mb-4">
              Are you sure you want to log out?
            </p>
            <div className="flex justify-center gap-4">
              <button
                className="bg-green-700 text-white px-4 py-2 rounded hover:bg-green-600"
                onClick={handleLogout}
              >
                Yes
              </button>
              <button
                className="bg-gray-300 text-gray-700 px-4 py-2 rounded hover:bg-gray-400"
                onClick={() => setShowLogoutConfirmation(false)}
              >
                No
              </button>
            </div>
          </div>
        </div>
      )}

      {showDeleteConfirmation && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
          <div className="bg-white rounded-lg shadow-lg p-6">
            <p className="text-center text-gray-800 mb-4">
              Are you sure you want to delete your profile?
            </p>
            <div className="flex justify-center gap-4">
              <button
                className="bg-green-700 text-white px-4 py-2 rounded hover:bg-green-600"
                onClick={handleDelete}
              >
                Yes
              </button>
              <button
                className="bg-gray-300 text-gray-700 px-4 py-2 rounded hover:bg-gray-400"
                onClick={() => setShowDeleteConfirmation(false)}
              >
                No
              </button>
            </div>
          </div>
        </div>
      )}
      <UpdateProfile
        isOpen={showProfileUpdate}
        onClose={() => setShowProfileUpdate(false)}
      />
    </header>
  );
}