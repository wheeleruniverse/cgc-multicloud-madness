'use strict'

const axios = require('axios');
const auth = require('creds/auth.json');


// AWS Lambda entry point
exports.handler = async (event) => {
    return await queryAzureAnalysisTable(event)
}

const queryAzureAnalysisTable = async function(event) {
    
    const filter = `PartitionKey%20eq%20'${randomNumberBetween(1, 10)}'`
    console.log(`filter: ${filter}`);

    axios.get(auth.url, {
        headers: {
            'Accept': 'application/json;odata=nometadata'
        },
        params: {
            '$filter': filter,
            '$top': '50',
            'si': auth.si,
            'sig': auth.sig,
            'sv': auth.sv,
            'tn': auth.tn,
        }
    })
    .then(function (response) {
        console.log(response);
        console.log(`Found ${response.length} Records`);
        return response;
    })
    .catch(function (error) {
        console.error(error);
        return error;
    })
}


/**
 * returns a random number between min (inclusive) and max (exclusive)
 */
function randomNumberBetween(min, max) {  
    return Math.floor(Math.random() * (max - min) + min)
}

