'use strict'

const auth = require('creds/auth.json');
const https = require('https')

// AWS Lambda entry point
exports.handler = async (event) => {
    return await queryAzureAnalysisTable(event)
}

const queryAzureAnalysisTable = async function(event) {
    
    const limit = `$filter=PartitionKey%20eq%20'${randomNumberBetween(1, 10)}'&$top=50`
    console.log(`limit: ${limit}`);
    
    const options = {
        headers: {
            'Accept': 'application/json;odata=nometadata'
        },
        host: auth.Host,
        path: `${auth.Path}&${limit}` 
    }
    
    let response;
    const req = https.request(options, res => {
        console.log(`statusCode: ${res.statusCode}`)

        res.on('data', d => {
            response = d.value;
        })
    })

    req.on('error', error => {
        console.error(error)
        response = [];
    })

    req.end()
    
    console.log(`Found ${response.length} Records`);
    return response;
}


/**
 * returns a random number between min (inclusive) and max (exclusive)
 */
function randomNumberBetween(min, max) {  
    return Math.floor(Math.random() * (max - min) + min)
}

