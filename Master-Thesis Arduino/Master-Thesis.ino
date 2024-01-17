//Pin für Gerät ferstlegen
#define EMS8 8
#define VIB9 9

void setup()
{
    // Kommunikation zwischen Arduino und Computer --> 9600 ist die Geschwindikeit der Kommunikation
    Serial.begin(9600);
    // Wartet 2 Sekunden, um auf Daten zu warten, die dem Arduino gesendet werden
    Serial.setTimeout(2000);
    // Pin 8 und 9 werden dazu verwendet, um Signale zu senden
     pinMode(VIB9, OUTPUT);
     pinMode(EMS8, OUTPUT);
     //Pin 8 wird eingeschaltet 
     digitalWrite(EMS8, HIGH);
}

void loop()
{
  switch(Serial.read())
  {
        case 'A':
            digitalWrite(EMS8, LOW);
            delay(500);
            digitalWrite(EMS8, HIGH);
            break;

        case 'Z':
            digitalWrite(VIB9, HIGH);
            delay(500);
            digitalWrite(VIB9, LOW);
            break;
       
	case 'B': // Fall B: EMS und Vibration gleichzeitig aktivieren
            digitalWrite(EMS8, LOW); // EMS aktivieren
            digitalWrite(VIB9, HIGH); // Vibration aktivieren
            delay(500); // Beide für 1 Sekunde aktivieren
            digitalWrite(EMS8, HIGH);  // Beide deaktivieren
            digitalWrite(VIB9, LOW);
        break;
  }
}