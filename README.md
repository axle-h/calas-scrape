# calas-scrape

Scraper for the Calas customer portal.

Scrapes contacts by their index and all attached enquiries. Dumps everything to CSV files.

## Usage

```bash
Usage: Calas.Scrape [options]

Options:
  -h|--help                       Show help information
  -s|--start-index <START-INDEX>  The zero based start index
  -l|--limit <LIMIT>              The maximum number of records to scrape
  -u|--username <USERNAME>        Your Calas username
  -p|--password <PASSWORD>        Your Calas password
```

## Example

```bash
./Calas.Scrape -u your-email-address -p your-password -s 0 -l 100
```
