Process Json-Ld serializations into models and run through dotLiquid to generate HTML

##Example usage:


### Apply template 'test.html' to {'hello':'world'} and write to stdout

```
   js2html --json "{'hello':'world'}" --template test.html
```

### Apply template 'page.html' to *.json in templatedir, writing .html files to /html

```
   js2html --file *.json --templatedir /templates --output /html --template page.html
```
 
 
##Templating examples

Input Json-Ld. 

~~~ .json
{
    "@id": "http://ld.nice.org.uk/prov/entity#98ead3d:qualitystandards/qs70/st2/Statement.md",
    "@type": ["prov:Entity", "owl:NamedIndividual"],
    "qualitystandard:setting": "http://ld.nice.org.uk/ns/qualitystandard/setting#Primary care setting",
    "qualitystandard:targetPopulation": ["http://ld.nice.org.uk/ns/qualitystandard/agegroup#Children 1-15 years", "http://ld.nice.org.uk/ns/qualitystandard/agegroup#Young people", "http://ld.nice.org.uk/ns/qualitystandard/conditiondisease#Urinary incontinence"],
    "dcterms:abstract": "Children and young people have an agreed review date if they, or their\nparents or carers, are given advice about changing their daily routine\nto help with bedwetting.",
    "dcterms:title": "Quality statement 2: Review after initial advice is given",
    "prov:specializationOf": "resource:FF2ED3E529E5BDCDED86C24D3277F8A156814492DFE352A09804DF3228109D63",
    "_id": "resource:FF2ED3E529E5BDCDED86C24D3277F8A156814492DFE352A09804DF3228109D63",
    "_type": "qualitystatement"
}
---

Property names are transformed before injection into the template engine. '@' and ':' are removed so that it can render IRI properties. Property name access in dotLiquid is case insensitive, so you can use convention to indicate namespace - dcterms:Abstract can become dctermsAbstract.

##Example template

~~~ .html
<div>{{@id}}</div>
<div>{{_id}}</div>
<div>{{qualitystandardSetting}}</div>
<div>{{dctermsAbstract}}</div>
<ul>
{% for item in type -%}
    <li>{{item}}</li>
{% endfor -%}
</ul>
---

Applying this template to the sample Json-Ld produces

~~~ .html
<div>http://ld.nice.org.uk/prov/entity#98ead3d:qualitystandards/qs70/st2/Statement.md</div>
<div>resource:FF2ED3E529E5BDCDED86C24D3277F8A156814492DFE352A09804DF3228109D63</div>
<div>http://ld.nice.org.uk/ns/qualitystandard/setting#Primary care setting</div>
<div>Children and young people have an agreed review date if they, or their
parents or carers, are given advice about changing their daily routine
to help with bedwetting.</div>
<ul>
    <li>prov:Entity</li>
    <li>owl:NamedIndividual</li>
</ul>
---
