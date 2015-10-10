# SierraECG for JAWS Framework
This is a reference implementation of the SierraECG for JavaScript code running as
a JAWS Framework AWS Module (AWSM).

Live Demo (if you have curl):
```
$ curl -X POST -H "Content-Type: application/xml" -d @129DYPRG.xml \
  https://2yu6m1tgki.execute-api.us-east-1.amazonaws.com/dev/sierraecg/decode
```

## What is JAWS?
JAWS is the key to a serverless future. JAWS takes AWS Lambda and AWS API Gateway
and makes merging the two of them seamless(ish). Using JAWS, there is now a
(poor man's) reference implementation of a SierraECG decoding microservice.

## Installation
You should follow all of the steps at the [JAWS Framework](https://github.com/jaws-framework/JAWS) site to get your local setup
going. Then you can bug me to make this work with the latest AWSM.
