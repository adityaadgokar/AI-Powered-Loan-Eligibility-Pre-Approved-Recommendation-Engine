# AI-Powered Loan Eligibility & Pre-Approved Recommendation Engine (POC)

An end-to-end intelligent lending proof-of-concept built using **Google Gemini Multimodal OCR**, **.NET 8 Web API**, **C# Financial Decision Engine**, **Angular 18**, and **SQL Server / Entity Framework Core**.

---

## 🏛️ Architecture & Single Responsibility Principle (SRP)

This application strictly separates document intelligence from financial decision-making:
- **Gemini OCR (`GeminiDocumentExtractorService`)**: Responsible *solely* for extracting structured data from bank statement PDFs/images without making lending judgments.
- **Statement Validation (`StatementValidationService`)**: Responsible for auditing mathematical consistency and transaction integrity.
- **Financial Analysis Engine (`FinancialAnalysisEngine`)**: Deterministically calculates verified monthly income, essential expenses, existing debt obligations, and average balances.
- **Loan Affordability Engine (`LoanAffordabilityEngine`)**: Computes standard EMI math, enforces the 40% FOIR (Fixed Obligation to Income Ratio) policy, and determines the maximum pre-approved loan amount.
- **Persistence (`LoanDbContext`)**: Stores customer records, uploaded statements, transactions, and final audit results to SQL Server.
- **Frontend (`Angular 18`)**: An interactive dashboard showing requested loan parameters, financial snapshot cards, the pre-approved recommendation comparison, and the extracted transaction table.

---

## 🧮 Mathematical Calculations & Policy Rules

### 1. Equated Monthly Installment (EMI) Formula
$$EMI = \frac{P \times r \times (1+r)^n}{(1+r)^n - 1}$$
Where:
- $P$ = Loan principal (e.g. ₹5,00,000)
- $r$ = Monthly interest rate ($\frac{12\%}{12 \times 100} = 0.01$)
- $n$ = Number of monthly installments (e.g. 60 months)

### 2. Available Capacity & FOIR Limit
- Maximum Allowed Total EMI = $\text{Monthly Income} \times 40\% = ₹75,000 \times 40\% = ₹30,000$
- Existing EMIs = $₹10,000$
- **Available EMI Capacity** = $₹30,000 - ₹10,000 = ₹20,000$

### 3. Pre-Approved Principal Derivation
Solving the EMI formula for $P$:
$$P = \frac{\text{Capacity} \times \left((1+r)^n - 1\right)}{r \times (1+r)^n}$$
At $₹20,000/\text{month}$, $12\%$ interest, and $60$ months $\rightarrow$ **₹8,99,100 ($\approx$ ₹8.9 Lakhs)** pre-approved recommendation!

---

## 🚀 How to Run the POC

### 1. Configure Settings

#### Backend (`backend/appsettings.json`)
The connection string and Gemini model are configured:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=.;Initial Catalog=LoanRecommendationDb;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "Gemini": {
    "ApiKey": "<YOUR_GEMINI_API_KEY>",
    "Model": "gemini-2.5-flash",
    "PromptFilePath": "Prompts/BankStatementOcrPrompt.txt"
  }
}
```

> **Prompt Customization**: The OCR prompt is decoupled from code and located at **[`backend/Prompts/BankStatementOcrPrompt.txt`](file:///d:/Winjit%20POC/backend/Prompts/BankStatementOcrPrompt.txt)**. You can edit the prompt instructions, JSON structure, or extraction rules directly in this file without recompiling the application.

#### Frontend Environment (`frontend/src/environments/environment.ts`)
The API base URL is configured in the environment file:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

### 2. Start the .NET 8 Backend API
In a terminal:
```bash
cd backend
dotnet run --urls "http://localhost:5000"
```
- Swagger API Docs will be available at: **http://localhost:5000/swagger**

### 3. Start the Angular 18 Frontend
In a separate terminal:
```bash
cd frontend
npm start
```
- Angular App will be available at: **http://localhost:4200**

---

## 🧪 Testing the Real Scenario
1. Open **http://localhost:4200**.
2. Upload a bank statement PDF or image (for example, the provided [`sample_bank_statement.pdf`](file:///d:/Winjit%20POC/sample_bank_statement.pdf)).
3. Enter loan parameters:
   - Requested Loan: **₹5,00,000**
   - Tenure: **60 months**
   - Interest Rate: **12.0%**
4. Click **"⚡ Analyze Statement & Evaluate Loan"**:
   - The document is processed by **Gemini OCR** (`gemini-2.5-flash`) using the prompt template in `backend/Prompts/BankStatementOcrPrompt.txt`.
   - The backend validates transaction integrity, calculates income (**₹75,000**), living expenses (**₹25,000**), existing EMIs (**₹10,000**), and disposable income (**₹40,000**).
   - Under the 40% FOIR policy, the engine evaluates affordability and recommends an enhanced pre-approved loan of **₹8,90,000** (~₹8.9L) instead of just ₹5,00,000.
   - All results and extracted transactions are rendered on the Angular dashboard and persisted to SQL Server.

---

## 🔬 Unit Tests
Run the automated xUnit tests to verify calculation precision:
```bash
dotnet test tests
```
All 4 tests cover EMI calculations, inverse loan principal derivation, FOIR rule constraints, and transaction categorization.

---

## 🔌 External Application Integration Endpoint

A dedicated, lightweight endpoint is available for third-party applications to evaluate loan pre-approvals without any UI or full statement payload overhead.

### Endpoint Details
- **Method**: `POST`
- **Route**: `/api/loan/pre-approval`
- **Content-Type**: `multipart/form-data`

### Request Parameters (Form-Data)
| Parameter | Type | Required | Description |
|---|---|---|---|
| `File` | Binary File | Yes | Bank statement or payslip (PDF, PNG, JPG) |
| `RequestedLoan` | Decimal | Yes | Desired loan amount (e.g. `500000`) |
| `Tenure` | Integer | Yes | Repayment duration in months (e.g. `36`) |
| `InterestRate` | Decimal | Yes | Interest rate % p.a. (e.g. `10.5`) |

### Example cURL
```bash
curl -X POST "http://localhost:5000/api/loan/pre-approval" \
  -F "File=@sample_bank_statement.pdf" \
  -F "RequestedLoan=500000" \
  -F "Tenure=36" \
  -F "InterestRate=10.5"
```

### Response Example (`200 OK`)
```json
{
  "customerName": "Rahul Sharma",       // Extracted name, or "NA" if not retrievable
  "accountNumber": "987654321012",     // Extracted account number, or "NA" if not retrievable
  "status": "Approved",
  "approvedAmount": 500000.0,
  "preApprovedPotentialOffer": 750000.0,
  "monthlyIncome": 85000.0,
  "monthlyDisposableIncome": 48000.0,
  "monthlyEmiCanPay": 24000.0,
  "requestedLoan": 500000.0,
  "tenure": 36,
  "interestRate": 10.5,
  "monthlyEmi": 16254.0,
  "fixedDeduction": 10000.0,            // Existing loan EMIs
  "variableDeduction": 27000.0,         // Personal & living expenses
  "totalCredit": 255000.0,              // Total credit inflows in statement
  "totalDebit": 111000.0,               // Total debit outflows in statement
  "totalAvailableBalance": 144000.0,    // Current / closing account balance
  "categoryBreakdown": {
    "salary": 225000.0,
    "rent": 45000.0,
    "groceries": 18000.0,
    "shopping": 7000.0,
    "utilityBills": 7000.0,
    "loanEmi": 30000.0,
    "medicalExpenses": 0.0,
    "others": 80000.0
  },
  "categorizedTransactions": [
    {
      "date": "2026-01-01",
      "description": "SALARY CREDIT - ACME TECH LTD",
      "debit": 0.0,
      "credit": 75000.0,
      "balance": 110000.0,
      "category": "Salary"
    },
    {
      "date": "2026-01-03",
      "description": "RENT PAYMENT TO LANDLORD",
      "debit": 15000.0,
      "credit": 0.0,
      "balance": 95000.0,
      "category": "Rent"
    }
  ],
  "remarks": "Customer is eligible for requested loan of ₹500,000. Pre-approved potential offer up to ₹750,000 is available."
}
```
*(If `customerName` or `accountNumber` cannot be found or read from the uploaded document, both fields explicitly return `"NA"` without any placeholder defaults).*
*(If disapproved, `status` returns `"Disapproved"`, `approvedAmount` returns `0`, and `preApprovedPotentialOffer` shows the maximum potential loan amount the customer can qualify for).*

