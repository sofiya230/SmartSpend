<p align="center">
  <img src="https://github.com/sofiya230/SmartSpend/raw/main/SmartSpend.Web/wwwroot/icon.svg" alt="SmartSpend Logo" width="200">
</p>

# SmartSpend

**Your finances. Your control. Your rules.**

SmartSpend is a full-featured, open-source personal finance manager built with ASP.NET Core. It allows you to import bank data, categorize transactions, manage budgets, and view insightful reports — all hosted and controlled by you.

---

## 🚀 Features

- Import bank and credit card statements (OFX / XLSX)
- Auto-categorize transactions using rules or patterns
- Monthly and yearly budget tracking
- Advanced reporting and visualizations
- Attach receipts to transactions
- Split single transactions across multiple categories
- Bulk category reassignments
- Full REST API access to all reports and data
- Export any data to Excel
- Secure user authentication and roles (Admin/User)
- Mobile-friendly responsive interface

---

## 🛠️ Build & Run

To run SmartSpend locally:

```bash
dotnet build
dotnet ef database update
dotnet run --project SmartSpend.Web
