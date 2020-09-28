# Test_Analyticalways
Proyecto de consola para migracion masiva de datos

## Comenzando 🚀
Estas instrucciones te permitirán obtener una copia del proyecto en funcionamiento en tu máquina local para propósitos de desarrollo y pruebas.

### Pre-requisitos 📋

Necesitas tener instalado Sql Server 2014 y visual studio 2019

### Instalación 🔧

1. Descargar el Zip y extraer las carpetas o sincronizarlo con tu cuenta de github en el Team Explorar

2. Dentro del archivo Test_Analyticalways se encuentra el script; script_Analyticalways.sql este se debe correr en el sql server management studio para crear la base de datos de nombre "Test_AnalyticalwaysDB" y en ella tambien se crea la tabla "Store" donde se insertaran los datos de la migracion.

3. Abrir la solucion Test_Analyticalways.sln dandole click directamente o con abrir la solucion desde visual studio 2019 la aplicacion para ejecutar va a solicitar permiso de administrador o puede ejecutar el visual estudio como administrador.

4. Abierto el proyecto se debe agregar las credenciales de la base de datos en las variables esto permite que la aplicacion se conecte a DB y poder insertar los datos.
                
        private static string ServerName = "";
        private static string Database = "Test_AnalyticalwaysDB";
        private static string UserId = "";
        private static string Password = "";        
        
5. compilar la solucion y ejecutar.

## Ejecutando las pruebas ⚙️

Se realizaron varias ejecuciones con diferentes codigos intecionalmente se dejaron las funciones y los condigos comentados de las pruebas que se realizaron para para comparar los resultados de los tres formas que se me ocurrieron para realizar la migraciones de los dato.

por razones de redimiento descarte cualquier ciclo dentro de C# que pudiera generar multiples conxiones a la base de datos.

Quede con dos approaches una que ejecuta un store procedure  envio una tabla a travez de un type table al dodne creo el sp al momento de realizar la migracion y paso la tabla dejando al motor de sql realizar el insert en la tabla Store.

el segundo approach es usando SqlBulkCopy es bastante eficiente pero el hecho de usar un datatable y que el archivo en CSV tenia que migrarse a esta, mas el tiempo de descarga del archivo lo seguia haciendo lento.

Y buscando como optimizar mas la migracion pense en el tercer approach que consite en hacer el Bulk copy desde sql y enviarle el archivo CSV la tabla Store le realizo un Drop table esto no es nada util en caso de que la tabla tenga otras claves foraneas pero para el caso de esta evaluacion es muy ultil ya que cuando la tabla esta llena se logra reducir hasta 40 segundos

Esas fueron las pruebas realizadas.

## Autor ✒️

Andres Lacruz

Muchas Gracias por darme la oportunidad de participar, espero que les guste el pequeño resumen🤓.

Saludos.



