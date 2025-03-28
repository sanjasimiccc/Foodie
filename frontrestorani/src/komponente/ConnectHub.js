import React, { useEffect, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';

const ConnectHub = ({ restaurantName }) => {
    const [discountMessage, setDiscountMessage] = useState(null);
    const [connection, setConnection] = useState(null);

    useEffect(() => {
        // Kreiraj SignalR konekciju
        const connection = new HubConnectionBuilder()
            .withUrl('http://localhost:5018/restaurantHub')  // URL tvoje ASP.NET Core aplikacije
            .build();

        // Definiši callback koji će obraditi poruke o popustima
        connection.on('ReceiveDiscount', (message) => {
            setDiscountMessage(message);  // Ažuriraj poruku sa popustom
        });

        // Pokreni SignalR konekciju
        connection.start()
            .then(() => {
                console.log('SignalR connection established');
                connection.invoke('SubscribeToDiscounts', restaurantName);
            })
            .catch(err => console.error('SignalR connection error: ', err));

        setConnection(connection);

        // Cleanup pri unmount-u
        return () => {
            if (connection) {
                connection.stop();
            }
        };
    }, [restaurantName]);

    return (
        <div>
            {discountMessage ? (
                <div className="discount-alert">
                    <p>{discountMessage}</p>
                </div>
            ) : (
                <p>No discount updates</p>
            )}
        </div>
    );
};

export default ConnectHub;
