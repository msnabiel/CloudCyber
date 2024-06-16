import React, { useState, useEffect } from 'react';
import axios from 'axios';

function App() {
    const [monitorData, setMonitorData] = useState([]);

    useEffect(() => {
        fetchMonitorData();
    }, []);

    const fetchMonitorData = async () => {
        try {
            const response = await axios.get('http://localhost:5000/monitor-data'); // Replace with your backend endpoint
            setMonitorData(response.data);
        } catch (error) {
            console.error('Error fetching monitor data:', error);
        }
    };

    return (
        <div className="App">
            <h1>Google Drive Monitor</h1>
            <div className="monitor-data">
                {monitorData.map((item, index) => (
                    <div key={index} className="monitor-item">
                        <p>File Name: {item.fileName}</p>
                        <p>File ID: {item.fileId}</p>
                        <p>Last Modified: {item.lastModified}</p>
                        {/* Add more details as needed */}
                    </div>
                ))}
            </div>
        </div>
    );
}

export default App;
