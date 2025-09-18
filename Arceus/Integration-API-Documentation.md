# CoreLedger Integration APIs

This document outlines the comprehensive set of APIs to replace the Odoo integration with the CoreLedger system.

## Overview

The CoreLedger Integration APIs provide a complete financial ledger system for the Presto platform, replacing Odoo with a double-entry bookkeeping system that handles:

- Customer wallet management
- Order processing and commission distribution
- Driver earnings and settlements
- Vendor/Partner financial tracking
- Restaurant earnings management

## Base URL
```
/api/integration
```

## Authentication
All endpoints require appropriate authentication (implementation depends on your auth system).

---

## 🧑‍💼 Customer Management APIs

### Create Customer
**POST** `/customers`

Creates a new customer with an associated wallet account.

**Request:**
```json
{
  "fullName": "John Doe"
}
```

**Response:**
```json
{
  "customerId": 123,
  "walletAccountId": 456
}
```

### Update Customer
**PUT** `/customers/{id}`

Updates customer information.

**Request:**
```json
{
  "fullName": "John Smith"
}
```

### Get Customer
**GET** `/customers/{id}`

Retrieves customer details including wallet balance.

**Response:**
```json
{
  "id": 123,
  "fullName": "John Doe",
  "createdAt": "2024-01-01T00:00:00Z",
  "walletAccountId": 456,
  "walletBalance": 150.50
}
```

### Get Customer Wallet Balance
**GET** `/customers/{id}/wallet-balance`

**Response:**
```json
{
  "balance": 150.50
}
```

---

## 📦 Order Management APIs

### Create Jet Order (Grocery/Marketplace)
**POST** `/orders/jet`

Processes a Jet order payment with commission distribution.

**Request:**
```json
{
  "customerId": 123,
  "orderId": 789,
  "totalAmount": 100.00,
  "driverShare": 15.00,
  "partnerShare": 70.00,
  "companyShare": 15.00,
  "driverId": 456,
  "partnerId": 789,
  "companyId": 1
}
```

### Create Eat Order (Restaurant)
**POST** `/orders/eat`

Processes a restaurant order payment.

**Request:**
```json
{
  "customerId": 123,
  "orderId": 789,
  "totalAmount": 50.00,
  "driverShare": 8.00,
  "restaurantShare": 35.00,
  "companyShare": 7.00,
  "driverId": 456,
  "restaurantId": 789,
  "companyId": 1
}
```

### Create Vendor Order
**POST** `/orders/vendor`

Processes a vendor marketplace order.

**Request:**
```json
{
  "customerId": 123,
  "orderId": 789,
  "totalAmount": 75.00,
  "driverShare": 10.00,
  "vendorShare": 55.00,
  "companyShare": 10.00,
  "driverId": 456,
  "vendorId": 789,
  "companyId": 1
}
```

### Cancel Order
**POST** `/orders/{orderId}/cancel`

Cancels an order and processes refund.

**Request:**
```json
{
  "refundAmount": 100.00,
  "companyId": 1
}
```

### Return Order
**POST** `/orders/{orderId}/return`

Processes order return and refund.

**Request:**
```json
{
  "returnAmount": 50.00,
  "companyId": 1
}
```

---

## 💳 Wallet Management APIs

### Charge Wallet
**POST** `/wallet/charge`

Adds funds to customer wallet from external payment.

**Request:**
```json
{
  "customerId": 123,
  "amount": 100.00,
  "paymentToken": "pay_token_123",
  "companyId": 1
}
```

**Response:**
```json
{
  "transactionId": 789,
  "paymentTransactionId": "ext_pay_456",
  "amount": 100.00
}
```

### Deduct Wallet Balance
**POST** `/wallet/deduct`

Deducts amount from customer wallet for order payment.

**Request:**
```json
{
  "customerId": 123,
  "amount": 50.00,
  "orderId": 789,
  "companyId": 1
}
```

**Response:**
```json
{
  "transactionId": 456,
  "remainingBalance": 50.00
}
```

### Get Wallet Balance
**GET** `/wallet/{customerId}/balance`

**Response:**
```json
{
  "customerId": 123,
  "balance": 150.50,
  "asOfDate": "2024-01-01T12:00:00Z"
}
```

### Get Wallet Transactions
**GET** `/wallet/{customerId}/transactions?page=1&pageSize=20`

**Response:**
```json
{
  "customerId": 123,
  "transactions": [
    {
      "transactionId": 789,
      "description": "Wallet top-up",
      "amount": 100.00,
      "type": "CREDIT",
      "createdAt": "2024-01-01T12:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 50
}
```

---

## 🚗 Driver Management APIs

### Create Driver
**POST** `/drivers`

Creates a new driver with revenue account.

**Request:**
```json
{
  "fullName": "Driver Smith"
}
```

**Response:**
```json
{
  "driverId": 456,
  "revenueAccountId": 789
}
```

### Add Driver Earnings
**POST** `/drivers/{driverId}/earnings`

Records driver earnings for a delivery.

**Request:**
```json
{
  "amount": 15.00,
  "orderId": 789,
  "companyId": 1
}
```

**Response:**
```json
{
  "transactionId": 123,
  "totalEarnings": 250.00
}
```

### Add Driver Reward
**POST** `/drivers/{driverId}/rewards`

Records driver incentives and bonuses.

**Request:**
```json
{
  "amount": 50.00,
  "rewardType": "Weekly Bonus",
  "companyId": 1
}
```

### Get Driver Details
**GET** `/drivers/{driverId}`

**Response:**
```json
{
  "id": 456,
  "fullName": "Driver Smith",
  "createdAt": "2024-01-01T00:00:00Z",
  "totalEarnings": 250.00
}
```

### Get Driver Earnings
**GET** `/drivers/{driverId}/earnings?fromDate=2024-01-01&toDate=2024-01-31`

**Response:**
```json
{
  "driverId": 456,
  "totalEarnings": 250.00,
  "earnings": [
    {
      "transactionId": 123,
      "description": "Delivery earnings for order #789",
      "amount": 15.00,
      "earnedAt": "2024-01-01T12:00:00Z"
    }
  ],
  "fromDate": "2024-01-01T00:00:00Z",
  "toDate": "2024-01-31T23:59:59Z"
}
```

### Get Driver Settlement Status
**GET** `/drivers/{driverId}/settlement-status`

**Response:**
```json
{
  "driverId": 456,
  "pendingAmount": 250.00,
  "status": "PENDING",
  "lastSettlementDate": "2024-01-01T00:00:00Z"
}
```

---

## 🏪 Vendor Management APIs

### Create Vendor
**POST** `/vendors`

Creates a new vendor with revenue account.

**Request:**
```json
{
  "vendorName": "ABC Store"
}
```

### Create Vendor Bill
**POST** `/vendors/{vendorId}/bills`

Creates a commission bill for vendor.

**Request:**
```json
{
  "amount": 500.00,
  "description": "Monthly commission",
  "companyId": 1
}
```

### Get Vendor Details
**GET** `/vendors/{vendorId}`

### Get Vendor Total Amount
**GET** `/vendors/{vendorId}/total-amount?fromDate=2024-01-01&toDate=2024-01-31`

**Response:**
```json
{
  "vendorId": 789,
  "totalSales": 1000.00,
  "commission": 150.00,
  "netPayout": 850.00,
  "fromDate": "2024-01-01T00:00:00Z",
  "toDate": "2024-01-31T23:59:59Z"
}
```

### Process Vendor Settlement
**POST** `/vendors/{vendorId}/settlement`

**Request:**
```json
{
  "settlementAmount": 850.00,
  "paymentReference": "BANK_TRANSFER_123",
  "companyId": 1
}
```

---

## 🍽️ Restaurant Management APIs

### Create Restaurant
**POST** `/restaurants`

Creates a new restaurant with revenue account.

**Request:**
```json
{
  "restaurantName": "Pizza Palace"
}
```

### Add Restaurant Earnings
**POST** `/restaurants/{restaurantId}/earnings`

Records restaurant earnings from orders.

**Request:**
```json
{
  "amount": 35.00,
  "orderId": 789,
  "companyId": 1
}
```

### Get Restaurant Details
**GET** `/restaurants/{restaurantId}`

### Get Restaurant Earnings
**GET** `/restaurants/{restaurantId}/earnings?fromDate=2024-01-01&toDate=2024-01-31`

### Get Restaurant Settlement Status
**GET** `/restaurants/{restaurantId}/settlement-status`

### Process Restaurant Settlement
**POST** `/restaurants/{restaurantId}/settlement`

**Request:**
```json
{
  "settlementAmount": 1500.00,
  "paymentReference": "BANK_TRANSFER_456",
  "companyId": 1
}
```

---

## Error Handling

All endpoints return standardized error responses:

**400 Bad Request:**
```json
{
  "error": "Insufficient wallet balance"
}
```

**404 Not Found:**
```json
{
  "error": "Customer not found"
}
```

**500 Internal Server Error:**
```json
{
  "error": "An error occurred while processing your request",
  "statusCode": 500,
  "timestamp": "2024-01-01T12:00:00Z"
}
```

---

## Key Features

### ✅ **Double-Entry Accounting**
- All transactions maintain accounting integrity
- Automatic journal entry creation
- Balance validation and consistency

### ✅ **Comprehensive Financial Tracking**
- Customer wallet management
- Multi-party commission distribution
- Vendor and restaurant settlement tracking
- Driver earnings and payouts

### ✅ **Integration Ready**
- RESTful API design
- Standardized request/response formats
- Comprehensive error handling
- Scalable architecture

### ✅ **Audit Trail**
- Complete transaction history
- Immutable financial records
- Reconciliation capabilities

This CoreLedger system provides a complete replacement for Odoo financial integration while offering better performance, simpler maintenance, and full control over the financial data.