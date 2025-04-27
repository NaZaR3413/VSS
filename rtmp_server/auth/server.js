const sql = require('mssql');
const express = require('express');
const app = express();
app.use(express.urlencoded({ extended: true }));

const { ManagedIdentityCredential } = require('@azure/identity');
const { SecretClient } = require('@azure/keyvault-secrets');

// azure secrets access
const credential = new ManagedIdentityCredential();
const vaultName = "VSS-keys";
const url = `https://${vaultName}.vault.azure.net`;
const client = new SecretClient(url, credential);

// Start async wrapper
(async () => {
  let dbConfig;
  try {
    // Load secrets from Key Vault
    dbConfig = {
      user: (await client.getSecret("DB-USER")).value,
      password: (await client.getSecret("DB-PASSWORD")).value,
      server: (await client.getSecret("DB-SERVER")).value,
      database: (await client.getSecret("DB-NAME")).value,
      options: {
        encrypt: true,
        trustServerCertificate: true
      },
      authentication: {
        type: 'default'
      }
    };
  } catch (err) {
    console.error("❌ Failed to retrieve secrets from Azure Key Vault:", err);
    return;
  }

  try {
    // Connect to the database
    await sql.connect(dbConfig);
    console.log('✅ Connected to the database successfully');

    // Define the /auth endpoint
    app.post('/auth', async (req, res) => {
      console.log('Received stream key:', req.body.name);
      const streamKey = req.body.name;

      try {
        const result = await sql.query`SELECT * FROM Livestream WHERE Id = ${streamKey}`;
        if (result.recordset.length > 0) {
          console.log('✅ Stream key is valid');

          const hlsUrl = `http://20.3.254.14:8080/hls/${streamKey}.m3u8`;

          await sql.query`
            UPDATE Livestream 
            SET HlsUrl = ${hlsUrl}, StreamStatus = 2
            WHERE Id = ${streamKey}`;

          res.status(200).send('OK');
        } else {
          console.log('❌ Invalid stream key');
          res.status(403).send('Invalid stream key');
        }
      } catch (err) {
        console.error('❌ Error querying the database:', err);
        res.status(500).send('Internal server error');
      }
    });

    // Start the server
    const PORT = process.env.PORT || 3000;
    app.listen(PORT, () => {
      console.log(`🚀 Auth server listening on port ${PORT}`);
    });

  } catch (err) {
    console.error('❌ Database connection failed:', err);
  }
})();
