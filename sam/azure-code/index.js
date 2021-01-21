'use strict'

// npm dependencies
const axios = require('axios');

// local dependencies
const auth = require('creds/auth.json');

// AWS Lambda entry point
exports.handler = async (event) => {
    return await queryAzureAnalysisTable(event)
}

const queryAzureAnalysisTable = async function(event) {
    
    const partition = randomNumberBetween(1, 10);
    const filter = `PartitionKey%20eq%20'${partition}'`
    console.log(`filter: ${filter}`);
    
    try {
        const url = `${auth.url}&$filter=${filter}&$top=50`
        const response = await axios.get(url, {
            headers: {
                'Accept': 'application/json;odata=nometadata'
            }
        })
        console.log(`HTTP ${response.status}`)
        
        const data = response.data.value;
        console.log(`Found ${data.length} Records`);
        
        return {
            Data: data,
            Partition: partition
        }
    } 
    catch (e) {
        console.error(e)
        return {
            Data: [],
            Error: JSON.stringify(e),
            Partition: partition
        }
    }
}


/**
 * returns a random number between min (inclusive) and max (exclusive)
 */
function randomNumberBetween(min, max) {  
    return Math.floor(Math.random() * (max - min) + min)
}

