#!/usr/bin/env bash

# Exit immediately if an unhandled error occurs or pipe fails
set -eo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
TIMESTAMP=$(date +%s)
PASSENGER_EMAIL="passenger_${TIMESTAMP}@example.com"
DRIVER_EMAIL="driver_${TIMESTAMP}@example.com"
PASSWORD="TestPassword123!"

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_step() {
    echo -e "\n${BLUE}===================================================${NC}"
    echo -e "${YELLOW}STEP: $1${NC}"
    echo -e "${BLUE}===================================================${NC}"
}

check_result() {
    if [ "$1" -eq 0 ]; then
        echo -e "${GREEN}✔ SUCCESS${NC}"
    else
        echo -e "${RED}✘ FAILED${NC}"
        exit 1
    fi
}

echo -e "${YELLOW}Testing Drive App Endpoints on: ${BASE_URL}${NC}"

# Check server reachable
if ! curl -s -f "${BASE_URL}/swagger/v1/swagger.json" > /dev/null 2>&1 && ! curl -s "${BASE_URL}/" > /dev/null 2>&1; then
    echo -e "${RED}Cannot connect to API server at ${BASE_URL}.${NC}"
    echo "Make sure your backend is running (e.g. 'dotnet run' in backend-api) and PostgreSQL is running."
    exit 1
fi

# -------------------------------------------------------------
log_step "1. Register Passenger"
# -------------------------------------------------------------
PASSENGER_REG_RES=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/auth/register" \
    -H "Content-Type: application/json" \
    -d "{
        \"fullName\": \"Test Passenger\",
        \"email\": \"${PASSENGER_EMAIL}\",
        \"password\": \"${PASSWORD}\",
        \"role\": \"Passenger\"
    }")
STATUS=$(echo "$PASSENGER_REG_RES" | tail -n 1)
BODY=$(echo "$PASSENGER_REG_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "2. Login Passenger"
# -------------------------------------------------------------
PASSENGER_LOGIN_RES=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/auth/login" \
    -H "Content-Type: application/json" \
    -d "{
        \"email\": \"${PASSENGER_EMAIL}\",
        \"password\": \"${PASSWORD}\"
    }")
STATUS=$(echo "$PASSENGER_LOGIN_RES" | tail -n 1)
BODY=$(echo "$PASSENGER_LOGIN_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

PASSENGER_TOKEN=$(echo "$BODY" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
PASSENGER_ID=$(echo "$BODY" | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Extracted Passenger Token: ${PASSENGER_TOKEN:0:20}..."
echo "Extracted Passenger ID: $PASSENGER_ID"

# -------------------------------------------------------------
log_step "3. Register Driver"
# -------------------------------------------------------------
DRIVER_REG_RES=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/auth/register" \
    -H "Content-Type: application/json" \
    -d "{
        \"fullName\": \"Test Driver\",
        \"email\": \"${DRIVER_EMAIL}\",
        \"password\": \"${PASSWORD}\",
        \"role\": \"Driver\"
    }")
STATUS=$(echo "$DRIVER_REG_RES" | tail -n 1)
BODY=$(echo "$DRIVER_REG_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "4. Login Driver"
# -------------------------------------------------------------
DRIVER_LOGIN_RES=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/auth/login" \
    -H "Content-Type: application/json" \
    -d "{
        \"email\": \"${DRIVER_EMAIL}\",
        \"password\": \"${PASSWORD}\"
    }")
STATUS=$(echo "$DRIVER_LOGIN_RES" | tail -n 1)
BODY=$(echo "$DRIVER_LOGIN_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

DRIVER_TOKEN=$(echo "$BODY" | grep -o '"token":"[^"]*' | cut -d'"' -f4)
DRIVER_ID=$(echo "$BODY" | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Extracted Driver Token: ${DRIVER_TOKEN:0:20}..."

# -------------------------------------------------------------
log_step "5. Create Driver Profile (POST /drivers)"
# -------------------------------------------------------------
PROFILE_RES=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/drivers" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{
        "licenseNumber": "DL-12345",
        "vehicleMake": "Toyota",
        "vehicleModel": "Corolla",
        "vehiclePlateNumber": "ABC-789-XY"
    }')
STATUS=$(echo "$PROFILE_RES" | tail -n 1)
BODY=$(echo "$PROFILE_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 201 ]
check_result $?

DRIVER_PROFILE_ID=$(echo "$BODY" | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Extracted Driver Profile ID: $DRIVER_PROFILE_ID"

# -------------------------------------------------------------
log_step "6. Get Driver Profile by ID (GET /drivers/{id})"
# -------------------------------------------------------------
GET_PROFILE_RES=$(curl -s -w "\n%{http_code}" -X GET "${BASE_URL}/drivers/${DRIVER_PROFILE_ID}" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}")
STATUS=$(echo "$GET_PROFILE_RES" | tail -n 1)
BODY=$(echo "$GET_PROFILE_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "7. Update Driver Status to Online (PATCH /drivers/{id}/status)"
# -------------------------------------------------------------
STATUS_RES=$(curl -s -w "\n%{http_code}" -X PATCH "${BASE_URL}/drivers/${DRIVER_PROFILE_ID}/status" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '"Online"')
STATUS=$(echo "$STATUS_RES" | tail -n 1)
BODY=$(echo "$STATUS_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "8. Update Driver Location (PATCH /drivers/{id}/location)"
# -------------------------------------------------------------
LOC_RES=$(curl -s -w "\n%{http_code}" -X PATCH "${BASE_URL}/drivers/${DRIVER_PROFILE_ID}/location" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{
        "latitude": 6.5244,
        "longitude": 3.3792
    }')
STATUS=$(echo "$LOC_RES" | tail -n 1)
BODY=$(echo "$LOC_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "9. Create Ride (POST /rides)"
# -------------------------------------------------------------
RIDE_RES=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/rides" \
    -H "Authorization: Bearer ${PASSENGER_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{
        "pickupLatitude": 6.5244,
        "pickupLongitude": 3.3792,
        "destinationLatitude": 6.6018,
        "destinationLongitude": 3.3515,
        "proposedFare": 2500,
        "rideType": "Economy"
    }')
STATUS=$(echo "$RIDE_RES" | tail -n 1)
BODY=$(echo "$RIDE_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 201 ]
check_result $?

RIDE_ID=$(echo "$BODY" | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Extracted Ride ID: $RIDE_ID"

# -------------------------------------------------------------
log_step "10. Get Ride by ID (GET /rides/{id})"
# -------------------------------------------------------------
GET_RIDE_RES=$(curl -s -w "\n%{http_code}" -X GET "${BASE_URL}/rides/${RIDE_ID}" \
    -H "Authorization: Bearer ${PASSENGER_TOKEN}")
STATUS=$(echo "$GET_RIDE_RES" | tail -n 1)
BODY=$(echo "$GET_RIDE_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "11. Get Pending Rides for Driver (GET /rides/pending)"
# -------------------------------------------------------------
PENDING_RES=$(curl -s -w "\n%{http_code}" -X GET "${BASE_URL}/rides/pending" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}")
STATUS=$(echo "$PENDING_RES" | tail -n 1)
BODY=$(echo "$PENDING_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "12. Driver Makes Offer (POST /rides/{rideId}/offers)"
# -------------------------------------------------------------
OFFER_RES=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/rides/${RIDE_ID}/offers" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{
        "offeredFare": 2750
    }')
STATUS=$(echo "$OFFER_RES" | tail -n 1)
BODY=$(echo "$OFFER_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

OFFER_ID=$(echo "$BODY" | grep -o '"id":"[^"]*' | cut -d'"' -f4)
echo "Extracted Offer ID: $OFFER_ID"

# -------------------------------------------------------------
log_step "13. Passenger Views Offers (GET /rides/{rideId}/offers)"
# -------------------------------------------------------------
GET_OFFERS_RES=$(curl -s -w "\n%{http_code}" -X GET "${BASE_URL}/rides/${RIDE_ID}/offers" \
    -H "Authorization: Bearer ${PASSENGER_TOKEN}")
STATUS=$(echo "$GET_OFFERS_RES" | tail -n 1)
BODY=$(echo "$GET_OFFERS_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "14. Passenger Accepts Offer (PATCH /rides/{rideId}/offers/{offerId}/accept)"
# -------------------------------------------------------------
ACCEPT_RES=$(curl -s -w "\n%{http_code}" -X PATCH "${BASE_URL}/rides/${RIDE_ID}/offers/${OFFER_ID}/accept" \
    -H "Authorization: Bearer ${PASSENGER_TOKEN}")
STATUS=$(echo "$ACCEPT_RES" | tail -n 1)
BODY=$(echo "$ACCEPT_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "15. Driver Starts Ride (PATCH /rides/{rideId}/start)"
# -------------------------------------------------------------
START_RES=$(curl -s -w "\n%{http_code}" -X PATCH "${BASE_URL}/rides/${RIDE_ID}/start" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}")
STATUS=$(echo "$START_RES" | tail -n 1)
BODY=$(echo "$START_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "16. Driver Completes Ride (PATCH /rides/{rideId}/complete)"
# -------------------------------------------------------------
COMPLETE_RES=$(curl -s -w "\n%{http_code}" -X PATCH "${BASE_URL}/rides/${RIDE_ID}/complete" \
    -H "Authorization: Bearer ${DRIVER_TOKEN}")
STATUS=$(echo "$COMPLETE_RES" | tail -n 1)
BODY=$(echo "$COMPLETE_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

# -------------------------------------------------------------
log_step "17. Alternative: Cancel Ride (Create new ride and cancel it)"
# -------------------------------------------------------------
CANCEL_RIDE_RES=$(curl -s -X POST "${BASE_URL}/rides" \
    -H "Authorization: Bearer ${PASSENGER_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{
        "pickupLatitude": 6.5000,
        "pickupLongitude": 3.3000,
        "destinationLatitude": 6.5500,
        "destinationLongitude": 3.3500,
        "proposedFare": 1500,
        "rideType": "Economy"
    }')
CANCEL_RIDE_ID=$(echo "$CANCEL_RIDE_RES" | grep -o '"id":"[^"]*' | cut -d'"' -f4)

CANCEL_ACTION_RES=$(curl -s -w "\n%{http_code}" -X PATCH "${BASE_URL}/rides/${CANCEL_RIDE_ID}/cancel" \
    -H "Authorization: Bearer ${PASSENGER_TOKEN}")
STATUS=$(echo "$CANCEL_ACTION_RES" | tail -n 1)
BODY=$(echo "$CANCEL_ACTION_RES" | head -n -1)
echo "Response ($STATUS): $BODY"
[ "$STATUS" -eq 200 ]
check_result $?

echo -e "\n${GREEN}===================================================${NC}"
echo -e "${GREEN}🎉 ALL ENDPOINTS TESTED SUCCESSFULLY!${NC}"
echo -e "${GREEN}===================================================${NC}"
