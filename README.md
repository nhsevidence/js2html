# JSON -> dotLiquid -> HTML

Process JSON object into models and run through dotLiquid to generate HTML

Example usage:

```
for f in $(find ../output/bnf/json/drug/ -name "*.json"); do mono js2html.exe --templatename drug --modelfile $f --outputfile "../output/browse/drug/`basename $f .json`.html"; done
```
