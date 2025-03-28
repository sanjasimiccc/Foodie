import {useContext, useEffect, useState} from "react";
import { AppContext } from "../App";
import { useNavigate } from "react-router-dom"
import ListaHrane from "../komponente/ListaHrane.js";
import UpdateRestoran from '../stranice/UpdateRestoran.js';

import { HubConnectionBuilder } from '@microsoft/signalr';


export default function StranicaRestoranHrana(){
    
        const [hrana, setHrana] = useState([])
        const [restoran, setRestoran] = useState(null)
        const korisnik = useContext(AppContext).korisnik
        const [showRestaurantUpdate, setShowRestaurantUpdate] = useState(false);

        const [stranicaZaPregled, setStranicaZaPregled] = useState(1);
        const [stranicaZaPrikaz, setStranicaZaPrikaz] = useState(1);
        const [kraj, setKraj] = useState(false);    

        const [connection, setConnection] = useState(null);
        const [isSubscribed, setIsSubscribed] = useState(false);
        const [messages, setMessages] = useState([]);  // Dodajemo stanje za poruke



        useEffect(() => {
            // Kreiraj SignalR konekciju sa serverom
            const newConnection = new HubConnectionBuilder()
                .withUrl('http://localhost:5018/restoranHub')  // URL do vašeg SignalR hub-a
                .build();
    
            setConnection(newConnection);

            //naknadno dodato: 
            // Uveriti se da se konekcija uspostavi odmah kad se komponenta učita
            const startConnection = async () => {
                try {
                    await newConnection.start();
                    console.log('Connection started.');
                } catch (error) {
                    console.error('Error while starting connection: ', error);
                }
            };

            startConnection();

            // Čistimo konekciju kad se komponenta unmount-uje
            return () => {
                if (newConnection) {
                    newConnection.stop();
                }
            };
        }, []);

        const handleSubscribe = async () => {
            const restoranID = sessionStorage.getItem('selectedRestoranId');
            
            if (!restoranID) {
                console.error("Restoran ID nije pronađen.");
                return;
            }
        
            try {
                if (connection && !isSubscribed) {
                    await connection.invoke('SubscribeToRestaurant', restoranID);
                    setIsSubscribed(true);
                    console.log('Subscribed to restaurant:', restoranID);
        
                    // Definišemo događaj za primanje poruka
                    connection.on('ReceiveDiscount', (message) => {
                        try {
                            console.log('Message:', message);
                            setMessages(prevMessages => [...prevMessages, message]);
                        } catch (error) {
                            console.error("Greška prilikom obrade poruke:", error);
                        }
                    });
                }
            } catch (error) {
                console.error('Greška prilikom pretplate:', error);
            }
        };

        // const handleSubscribe = async () => {
        //     const restoranID = sessionStorage.getItem('selectedRestoranId')
        //     try {
        //         if (connection) {
        //             await connection.start();  
        //             console.log('Connection started.');

        //             // Pretplati se na restoran
        //             await connection.invoke('SubscribeToRestaurant', restoranID);
        //             setIsSubscribed(true); 
        //             console.log('Subscribed to restaurant:', restoranID);


        //             // Definišemo događaj za primanje poruka
        //             connection.on('ReceiveDiscount', (message) => {
        //                 try {

        //                     console.log('Message:', message);
        //                     setMessages(prevMessages => [...prevMessages, message]); 
        //                 } catch (error) {
        //                     console.log("Greška prilikom obrade poruke:", error);
        //                     setMessages(prevMessages => [...prevMessages, message]);
        //                 }
        //         });
        //         }
        //     } catch (error) {
        //         console.error("Greška prilikom pretplate:", error);
        //     }
        // };

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


        const navigate = useNavigate()

        const handleAddFoodClick = () => {
            navigate('/dodajHranu')
        };

        const loadHranaRestorana = async () => {
            const restoranID = sessionStorage.getItem('selectedRestoranId')
            //const token = sessionStorage.getItem('jwt') //ustvari mi ni ne trebaa too sad, ups
            //preuzimam restoran
            try {
                //if(token != null){
                    const response = await fetch(`http://localhost:5018/api/Restorani/preuzmiRestoran/${restoranID}`)
                    console.log(restoranID)
                    if(response.ok){
                        const data = await response.json()
                        setRestoran(data)
                        console.log("Response:", data);
                    }
                    else 
                    {
                        window.alert("Doslo je do greske s restoranom, response nije ok: "+ await response.text())
                    }
               // }
            }
            catch(error){
                window.alert("Doslo je do greske: " + error.message)
            }
            //preuzimam hranu
            try {
               // if(token != null){
                    const response = await fetch(`http://localhost:5018/api/Restorani/preuzmiHranuRestorana/${restoranID}/${stranicaZaPregled}`)
                    console.log(restoranID)
                    if(response.ok){
                        const data = await response.json()
                        setHrana(data.lista)
                        setKraj(data.kraj)
                        setStranicaZaPrikaz(stranicaZaPregled)
                        console.log("Response:", data)

                    }
                    else 
                    {
                        window.alert("Doslo je do greske sa hranom, response nije ok: "+ await response.text())
                    }
               // }
            }
            catch(error){
                window.alert("Doslo je do greske: " + error.message)
            }
        }

        const handleBrisanjeRestorana = async () => {
            const restoranID = sessionStorage.getItem('selectedRestoranId')
            const token = sessionStorage.getItem('jwt')
            try {
                const response = await fetch(`http://localhost:5018/api/Restorani/obrisiRestoran/${restoranID}`, {
                    method: 'DELETE',
                    headers: {
                        Authorization: `bearer ${token}`
                      }
                })
                if (!response.ok) {
                    const errorMessage = await response.text();
                    console.error('Error deleting restaurant:', errorMessage);
                    alert(`Error: ${errorMessage}`);
                } else {
                    const result = await response.text();
                    alert('Restaurant deleted successfully')
                    console.log('Restaurant deleted successfully:', result);
                    navigate('/restoraniSvi')
                }
            } catch (error) {
                console.error('Error during deletion:', error);
                alert('An error occurred. Please try again later.');
            }
          }
    
        useEffect(() => {
            loadHranaRestorana()
        }, [stranicaZaPregled])

        const handleFoodDeleted = () => {
            loadHranaRestorana(); // Ponovo preuzmi podatke o hrani
        };

  
        return (
            <div className="flex flex-col items-center justify-center min-h-screen bg-green-100">
                {restoran ? (
                    <>
                        {/* Glavni div za informacije o restoranu i dugme */}
                        <div className="relative w-full max-w-4xl p-10 bg-green-100 shadow-md rounded-lg">
                            
                            {/* Dugme za pretplatu na restoran u gornjem levom kutu */}
                            {korisnik && (
                                <div className="absolute top-4 left-4">
                                    <button
                                        className="px-4 py-2 bg-green-700 text-white rounded-lg shadow hover:bg-green-800 transition"
                                        onClick={handleSubscribe}
                                        disabled={isSubscribed} // onemogući dugme ako je korisnik već pretplaćen
                                    >
                                        {isSubscribed ? 'Pretplaćeni ste!' : 'Pretplatite se na popuste'}
                                    </button>
                                </div>
                            )}
                            
                            
                            <div className="flex justify-end space-x-4 mb-4">
                                {korisnik != null && korisnik.jeAdmin && (
                                    <>
                                        <button
                                            className="px-4 py-2 bg-green-700 text-white rounded-lg shadow hover:bg-green-800 transition"
                                            onClick={() => setShowRestaurantUpdate(true)}
                                        >
                                            Update
                                        </button>
                                        <button
                                            className="px-4 py-2 bg-red-700 text-white rounded-lg shadow hover:bg-red-800 transition"
                                            onClick={handleBrisanjeRestorana} // Dodaj funkcionalnost za brisanje
                                        >
                                            Delete
                                        </button>
                                    </>
                                )}
                            </div>
        
                            {/* Informacije o restoranu */}
                            <div className="text-center">
                                <h1 className="text-7xl font-bold text-green-700 mb-1">{restoran.naziv}</h1>
                                <h2 className="text-5xl font-bold text-green-700 mb-2">{restoran.adresa}</h2>
                            </div>
                            <div>
                                <p className="text-xl text-green-800 mb-1">
                                    {restoran.opis} | Contact: {restoran.telefon}
                                    {restoran.prosecnaOcena !== 0 && ` | Rating: ${restoran.prosecnaOcena}`}
                                </p>
                            </div>
                            {/* Dugme "Add Food" */}
                            {korisnik != null && korisnik.jeAdmin && (
                                 <div className="flex justify-center">
                                 <button
                                     className="bg-green-700 text-white px-4 py-2 rounded-lg shadow hover:bg-green-800 transition mt-4"
                                     onClick={handleAddFoodClick}
                                 >
                                     Add Food
                                 </button>
                             </div>
                            )}
                        </div>
        
                        {/* Lista hrane */}
                        <div className="w-full max-w-4xl">
                            {hrana !== null && hrana != undefined && hrana.length > 0 ? (
                                <>
                                    <ListaHrane lista={hrana} onFoodDeleted={handleFoodDeleted}></ListaHrane>
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
                                <p className="text-center text-gray-600 mt-8">
                                    The restaurant currently has no food available for ordering!
                                </p>
                            )}
                        </div>

                        {/* Prikazivanje poruka */}
                        {messages.length === 0 ? (
                            <p>No messages yet.</p>
                        ) : (
                            <ul>
                                {messages.map((msg, index) => (
                                    <li key={index}>{typeof msg === 'string' ? msg : JSON.stringify(msg)}</li>
                                ))}
                            </ul>
                        )}



                    </>
                ) : (
                    <p className="text-center text-gray-600">Loading restaurant data...</p>
                )}
                <UpdateRestoran
                    isOpen={showRestaurantUpdate}
                    onClose={() => setShowRestaurantUpdate(false)}
                    restoran={restoran} // Prosleđivanje objekta restoran
                />
            </div>
            
        );
}