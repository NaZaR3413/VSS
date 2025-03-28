const sql = require('mssql');
const express = require('express');
const app = express();
app.use(express.urlencoded({ extended: true }));
const { DefaultAzureCredential } = require('@azure/identity');
const { SecretClient } = require('@azure/keyvault-secrets');

// Load environment variables from .env file
//require('dotenv').config();

const credential = new DefaultAzureCredential();
const vaultName = "VSS-keys";
const url = `https://${vaultName}.vault.azure.net`;

const client = new SecretClient(url, credential);

// Database configuration from environment variables
const dbConfig = {
  user: await client.getSecret("DB-USER"),
  password: await client.getSecret("DB-PASSWORD"),
  server: await client.getSecret("DB-SERVER"),
  database: await client.getSecret("DB-NAME"),
  options: { // may or may not be optional depending on env and OS
    encrypt: true,
    trustServerCertificate: true 
  },
  authentication: true === 'true' ? {
    type: 'default'
  } : undefined
};

// Connect to the database
sql.connect(dbConfig)
  .then(() => {
    console.log('Connected to the database successfully');

    // Define an endpoint to verify the stream key
    app.post('/auth', async (req, res) => {
      console.log('Received stream key:', req.body.name);  // Debugging log to see incoming requests
      const streamKey = req.body.name;  // OBS sends the stream key as `name`
  
      try {
          const result = await sql.query`SELECT * FROM Livestream WHERE Id = ${streamKey}`;
          if (result.recordset.length > 0) {
              console.log('Stream key is valid');

              // Construct the HLS URL dynamically using the stream key
              const hlsUrl = `http://20.3.254.14:8080/hls/${streamKey}.m3u8`;

              // Update the HlsUrl and StreamStatus in the database
              await sql.query`
                UPDATE Livestream 
                SET HlsUrl = ${hlsUrl}, StreamStatus = 2
                WHERE Id = ${streamKey}`;

                res.status(200).send('OK');  // Send 'OK' if the stream key is valid and table is properly updated
          } else {
              console.log('Invalid stream key');
              res.status(403).send('Invalid stream key');  // Send 403 if the stream key is not valid
          }
      } catch (err) {
          console.error('Error querying the database:', err);
          res.status(500).send('Internal server error');
      }
    });

  })
  .catch(err => {
    console.error('Database connection failed', err);
  });

// Start the server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
  console.log(`Auth server listening on port ${PORT}`);
});
