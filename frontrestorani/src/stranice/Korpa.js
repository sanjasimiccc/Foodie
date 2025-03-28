import React, { useEffect, useState } from 'react';

const Korpa = () => {
  const [korpa, setKorpa] = useState(null);
  const [error, setError] = useState(null);


  useEffect(() => {
    const fetchKorpa = async () => {
      try {
        const token = sessionStorage.getItem('jwt');
        const response = await fetch("http://localhost:5018/api/StavkaUKorpi/preuzmiKorpuSaCenom", {
          method: 'GET',
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          throw new Error(`Greška pri preuzimanju podataka: ${response.statusText}`);
        }
        else{
            const data = await response.json();
            setKorpa(data); 
        }
      } catch (err) {
        setError("Došlo je do greške pri preuzimanju podataka o korpi.");
        console.error(err);
      }
    };

    fetchKorpa();
  }, []);

  const ukloniStavku = async (hranaId) => {
    try {
      const token = sessionStorage.getItem('jwt');
      const response =await fetch(`http://localhost:5018/api/StavkaUKorpi/izbaciStavkuIzKorpe/${hranaId}`, {
        method: 'DELETE',
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
      });
     if (!response.ok) {
        throw new Error(`Greška pri brisanju stavke: ${response.statusText}`);
      }


      const updatedStavke = korpa.stavke.filter(stavka => stavka.hranaId !== hranaId);
      
   
      const updatedCena = updatedStavke.reduce((total, stavka) => total + stavka.ukupno, 0);


      setKorpa(prevKorpa => ({
        ...prevKorpa,
        stavke: updatedStavke,
        ukupnaCena: updatedCena,  
      }));
    } catch (err) {
      setError("Došlo je do greške pri brisanju stavke.");
      console.error(err);
    }
  };



  const poruciHranu = async () => {
    try {
      const token = sessionStorage.getItem('jwt');
      const response = await fetch("http://localhost:5018/api/StavkaUKorpi/poruciHranu", {
        method: 'DELETE',
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Greška pri poručivanju hrane");
      }

      alert("Porudžbina je uspešno poslata!");
      setKorpa({ stavke: [], ukupnaCena: 0 });
    } catch (err) {
      setError("Došlo je do greške pri poručivanju hrane.");
      console.error(err);
    }
  };


  const izbrisiSveIzKorpe = async () => {
    try {
      const token = sessionStorage.getItem('jwt');
      const response = await fetch("http://localhost:5018/api/StavkaUKorpi/IzbrisiSveIzKorpe", {
        method: 'DELETE',
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Greška pri brisanju svih stavki");
      }

      setKorpa({ stavke: [], ukupnaCena: 0 });
    } catch (err) {
      setError("Došlo je do greške pri brisanju stavki.");
      console.error(err);
    }
  };


  if (error) {
    return <div className="text-red-600 text-center p-4">{error}</div>;
  }


  if (!korpa) {
    return <div className="text-center p-4 text-gray-500">Učitavanje korpe...</div>;
  }


  const stavke = korpa.stavke || [];

  return (
    <div className="max-w-4xl mx-auto p-6 bg-white shadow-lg rounded-lg mt-10">
      <h2 className="text-3xl font-semibold text-center text-green-600 mb-6">Your Cart</h2>

      {stavke.length === 0 ? (
        <p className="text-center text-gray-500">Your cart is empty</p>
      ) : (
        <div>
          <table className="min-w-full table-auto border-collapse">
            <thead>
              <tr className="bg-green-700 text-white">
                <th className="px-4 py-2 text-left">Food</th>
                <th className="px-4 py-2 text-left">Price</th>
                <th className="px-4 py-2 text-left">Quantity</th>
                <th className="px-4 py-2 text-left">Total</th>
                <th className="px-4 py-2 text-left">Remove</th>
              </tr>
            </thead>
            <tbody>
              {stavke.map((stavka) => (
                <tr key={stavka.hranaId} className="border-b hover:bg-green-50">
                  <td className="px-4 py-2">{stavka.naziv}</td>
                  <td className="px-4 py-2">{stavka.cena} EUR</td>
                  <td className="px-4 py-2">{stavka.kolicina}</td>
                  <td className="px-4 py-2">{stavka.ukupno} EUR</td>
                <td className="px-4 py-2">
                     <button
                       className="text-red-600 hover:text-red-800"
                       onClick={() => ukloniStavku(stavka.hranaId)}
                     >
                       🗑️
                     </button>
                   </td> 

                </tr>
              ))}
            </tbody>
          </table>

          <div className="mt-4 flex justify-between text-xl font-semibold text-green-700">
            <h3>Total Price:</h3>
            <span>{korpa.ukupnaCena} EUR</span>
          </div>

          <div className="mt-6 flex justify-between">
            <button
              onClick={poruciHranu}
              className="px-6 py-2 bg-green-600 text-white font-semibold rounded-lg hover:bg-green-700"
            >
              Order Food
            </button>
            <button
              onClick={izbrisiSveIzKorpe}
              className="px-6 py-2 bg-red-600 text-white font-semibold rounded-lg hover:bg-red-700"
            >
              Clear Cart
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default Korpa;