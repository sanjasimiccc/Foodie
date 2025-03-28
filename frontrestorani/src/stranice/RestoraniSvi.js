//kad se logujes vodi nas na ovu stranicu gde su restorani odnosno vracamo sve restorane, ne top 10 (stranicenje dodati)
//pretrazivanje iznad mozda u heder
//treba da imamo dugme da se prikaze jedan restoran i to nas vodi na njegovu stranicu

import React, {useContext, useState, useEffect} from "react";
import { AppContext } from "../App";
import ListaRestorana from "../komponente/ListaRestorana";
import ConnectHub from "../komponente/ConnectHub";

export default function RestoraniSvi(){
    const [restorani, setRestorani] = useState([])
    const [searchTerm, setSearchTerm] = useState(""); // Stanje za unos pretrage
    const [filteredRestorani, setFilteredRestorani] = useState([]); // Stanje za rezultate pretrage
    const korisnik = useContext(AppContext).korisnik
    const [stranicaZaPregled, setStranicaZaPregled] = useState(1);
    const [stranicaZaPrikaz, setStranicaZaPrikaz] = useState(1);
    const [kraj, setKraj] = useState(false);    
    
    const handleNextPage = () => {
        if(!kraj){
            setStranicaZaPregled(stranicaZaPregled + 1)
        }
    }

    const handlePreviousPage = () => {
        if(stranicaZaPregled > 1){
            setStranicaZaPregled(stranicaZaPregled - 1)
        }
    }

    // Funkcija za učitavanje restorana sa servera
    const loadRestorani = async (query = "") => {
        try {
            const url = query
                ? `http://localhost:5018/api/Restorani/pretraziRestoranePoNazivu/${stranicaZaPregled}?naziv=${query}`
                : `http://localhost:5018/api/Restorani/preuzmiSveRestorane/${stranicaZaPregled}`;

            const response = await fetch(url);
            if (response.ok) {
                const data = await response.json();
                setRestorani(data.lista);
                setFilteredRestorani(data.lista);
                setKraj(data.kraj)
                setStranicaZaPrikaz(stranicaZaPregled)
                console.log("Response:", data);
            } else {
                window.alert("Došlo je do greške: " + (await response.text()));
            }
        } catch (error) {
            window.alert("Došlo je do greške: " + error.message);
        }
    };

    useEffect(() => {
        loadRestorani()
    }, [stranicaZaPregled])

    // Funkcija za ažuriranje unosa pretrage
    const handleSearchChange = (event) => {
        setSearchTerm(event.target.value);
    };

    // Funkcija za izvršavanje pretrage
    const handleSearch = () => {
        loadRestorani(searchTerm);
    };


    return (
        <div className="flex flex-col items-center justify-center min-h-screen bg-green-100">
             {/* Polje za pretragu */}
             <div className="w-full max-w-4xl mt-4 mb-4 flex items-center">
             <input
                type="text"
                placeholder="Search restaurants..."
                value={searchTerm}
                onChange={handleSearchChange}
                className="flex-grow px-4 py-2 border border-gray-300 rounded-l-lg shadow-sm focus:outline-none focus:ring-2 focus:ring-green-500 focus:border-transparent"
             />
             <button
                onClick={handleSearch}
                className="px-6 py-2 bg-green-700 text-white rounded-r-lg shadow-md hover:bg-green-800 transition-colors"
             >
             Search
             </button>
        </div>

         {/* Opis i dugme za Top 10 Restaurants */}
        <div className="w-full max-w-4xl mb-12 flex items-center justify-between">
            <p className="text-green-800 text-xl">
                Discover the best and most highly rated restaurants, recommended by our users...
            </p>
            {/* <button
                onClick={() => console.log()} //navigate na Top10 treba
                className="px-4 py-2 bg-green-700 text-white rounded-lg shadow-md hover:bg-green-800 transition-colors text-sm"
            >
                Top 10
            </button> */}
            <a
                href="/top10"
                className="text-green-700 underline hover:text-green-900 text-3xl font-medium"
            >
                View top 10
            </a>
        </div>

        {korisnik != null && korisnik.jeAdmin && (
            <div className="w-full max-w-4xl mb-12 flex items-center justify-between">
                <p className="text-green-800 text-xl">
                    Add restaurant...
                </p>
                <a
                    href="/dodajRestoran" // Proverite da li ova ruta odgovara vašoj aplikaciji
                    className="text-green-700 underline hover:text-green-900 text-3xl font-medium"
                >
                    Add restaurant
                </a>
            </div>
        )}

        <div className="text-center">
            <h1 className="text-7xl font-bold text-green-700 mt-10 mb-8">Explore Restaurants</h1>
        </div>
        <div className="w-full max-w-4xl">
            {restorani !== null && restorani.length > 0 ? (
            <>
                 <ListaRestorana lista={restorani}></ListaRestorana>
                 {/* Dugmići za stranicenje */}
                 <div className="flex justify-center mt-6 mb-6">
                    <button
                        onClick={handlePreviousPage}
                        className="px-4 py-2 rounded-lg shadow transition bg-green-700 text-white hover:bg-green-800 disabled:bg-gray-400 disabled:text-gray-800 disabled:cursor-not-allowed"
                        disabled={stranicaZaPregled === 1}
                    >
                        Previous
                    </button>
                    <span className="mx-4 text-green-800">{`Page: ${stranicaZaPrikaz}`}</span>
                    <button
                        onClick={handleNextPage}
                        className="px-4 py-2 rounded-lg shadow transition bg-green-700 text-white hover:bg-green-800 disabled:bg-gray-400 disabled:text-gray-800 disabled:cursor-not-allowed"
                        disabled={kraj}
                    >
                        Next
                    </button>
                </div>
            </>
            ) : (
               <p className="text-center text-gray-600">No restaurants available.</p>
            )}
        </div>
        </div>
    );
    
}