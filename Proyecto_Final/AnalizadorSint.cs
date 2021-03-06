﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_Final
{
    class AnalizadorSint
    {
        CArbol arbol;
        CNodo nodoAux = new CNodo();
        CNodo raiz;  // nodo raiz del arbol
        Stack pila;
        Tas tas = new Tas();  // ver so de cargas tas
        string[] lexemas; // array de lexemas obtenidos de lexer, seria los tipos de los lexemas

        public AnalizadorSint()
        {
            this.SetTas();
        }

        public void ApilarCampo(string campo) //apila el campo de la tabla cuando coincide con el buffer de entrada
        {
            if (campo != null)  // no hace falta poner en la tas los epsilon , ya que en campo nulo no hace nada
            {
                string[] separados;
                separados = campo.Split(); // si se olvida simbolos en tas no anda
                separados = separados.Where(i => i != "").ToArray();  // array con token separados segun el espacio (no poner " " con espacio), referencia no establecida el valor pos esta mal(tabla error ubicacion de simbolo)
                separados = separados.Reverse().ToArray();     // revierte el array para que sea mas facil apilarlo
                int longseparados = separados.Length;

                for (int c = 0; c < longseparados; c++)
                {
                    pila.Push(separados[c]);
                }
            }
        }

        public void CargarHojas(string campo, CNodo nodo)
        {
            if (campo != null)
            {
                string[] separados = campo.Split().Where(i => i != "").ToArray();
                if(separados[0].Equals("READ") || separados[0].Equals("WRITE"))
                {
                    int i = 2;
                    String valorString = "";
                    while (!separados[i].Equals(","))
                    {
                        valorString += separados[i] + " ";
                        i++;
                    }
                    int tam = separados.Length;
                    String[] separadosAux = { separados[0], separados[1], valorString, separados[tam - 3], separados[tam - 2], separados[tam - 1] };
                    separados = separadosAux;
                }
                for (int c = 0; c < separados.Length; c++)
                {
                    arbol.Insertar(separados[c], nodo);
                }
            }
            else
            {
                arbol.Insertar("", nodoAux);  //el dato va a estar vacio, representando epsilon
            }
        }


        public void InicializarPila()  //inicializa la pila y aplica el simbolo fin y el primer token no terminal de la gramatica, tambien coloca el nodo raiz que seria el primero de la gramatica
        {
            pila = new Stack();
            pila.Push("$"); // fin de la pila 
            pila.Push("PROG"); //no terminal inicial de la gramtica ll1
            arbol = new CArbol();
            raiz = arbol.Insertar("PROG", null);  // inicializa el arbol , al ponerle null lo inicializa como raiz al nodo
        }



        public bool Analizar()           // devuelve true si la expresion es correcta , lexemas en orden obtenidos del lexer
        {
            int longlexemas = lexemas.Length;
            //puntero  para recorrer lexemas 
            string tope = Convert.ToString(pila.Peek());
            bool estado = true;  // es true mientras no haya dos terminales diferentes en tope y lexema[0]
            string[] terminales = tas.GetTerminales();

            String valorDato = "";
            while (tope != "$" && lexemas[0] != "$" && estado)  // mientras top sea diferente de fin y la lista no este vacia
            {
                if (lexemas[0].Contains("STRING@") || lexemas[0].Contains("id@") || lexemas[0].Contains("num@") || lexemas[0].Contains("oprel@"))
                {
                    String[] lexemaAux = lexemas[0].Split('@');
                    lexemas[0] = lexemaAux[0];
                    valorDato = lexemaAux[1];
                }

                if (tope == lexemas[0]) // se van eliminando el primero del vector con lexemas
                {
                    eliminarPrimero();
                    pila.Pop();
                    tope = Convert.ToString(pila.Peek());
                }
                else if (EsTerminal(tope, terminales) && EsTerminal(lexemas[0], terminales) && tope != lexemas[0]) // para de analizar si tope != de lexema y ambos son terminales
                {
                    estado = false;
                }
                else
                {
                    int poscolumna = tas.PosicionTerminales(lexemas[0]);     //fila lo del buffer de entrada
                    int posfila = tas.PosicionNoTerminales(tope);       // columna lo de a pila
                    string valorPosTas = tas.Elemento(posfila, poscolumna);
                    pila.Pop();                // desapila el top
                    nodoAux = arbol.BuscarNoTratado(tope, raiz); // busca el nodo no tratado para luego insertarle los hijos
                    nodoAux.Tratado = true; // cambia el valor a true ya que va a insertar los datos en ese nodo
                    if (valorPosTas != null && (valorPosTas.Contains("READ") || valorPosTas.Contains("WRITE")))
                    {
                        String valorCampo = lexemas[2].Split('@')[1];
                        String nombreId = lexemas[4].Split('@')[1];
                        String nuevaValorPosTas = valorPosTas.Replace("STRING", valorCampo).Replace("id", nombreId);
                        CargarHojas(nuevaValorPosTas, nodoAux);
                    }
                    else if (valorPosTas != null && valorPosTas.Contains("id"))
                    {
                        String nuevaValorPosTas = valorPosTas.Replace("id", valorDato);
                        CargarHojas(nuevaValorPosTas, nodoAux);
                    }
                    else if (valorPosTas != null && valorPosTas.Contains("num"))
                    {
                        String nuevaValorPosTas = valorPosTas.Replace("num", valorDato);
                        CargarHojas(nuevaValorPosTas, nodoAux);
                    }
                    else if (valorPosTas != null && valorPosTas.Contains("oprel"))
                    {
                        String nuevaValorPosTas = valorPosTas.Replace("oprel", lexemas[1].Split('@')[1]);
                        CargarHojas(nuevaValorPosTas, nodoAux);
                    }
                    else 
                    {
                        CargarHojas(valorPosTas, nodoAux); // carga en el arbol el valor de la tas
                    }
                    ApilarCampo(valorPosTas); // apila lo correspondiente a la interseccion de fila y columna , si el valor es nulo no hace nada
                    tope = Convert.ToString(pila.Peek());
                }

            }
            if(tope != "$" && lexemas[0]=="$" && !EsTerminal(tope,terminales)) // verifica el epsilon del ante ultimo simbolo de la pila, si solo es no terminal
            {
                int poscolumna = tas.PosicionTerminales(lexemas[0]);     //fila lo del buffer de entrada
                int posfila = tas.PosicionNoTerminales(tope);       // columna lo de a pila
                string valorPosTas = tas.Elemento(posfila, poscolumna);
                pila.Pop();                // desapila el top
                ApilarCampo(valorPosTas); // apila lo correspondiente a la interseccion de fila y columna , si el valor es nulo no hace nada
                tope = Convert.ToString(pila.Peek());
            }
            if (tope == "$" && lexemas[0] == "$") 
            { return true; }
            else
            { return false; }
        }

        public string[] eliminarPrimero() // elimina primer elemento de un array
        {
            string[] nuevoLexema;
            int longlexema = this.lexemas.Length;

            nuevoLexema = new string[longlexema - 1];
            for (int c = 0; c < longlexema - 1; c++)
            {
                nuevoLexema[c] = lexemas[c + 1];
            }

            this.lexemas = nuevoLexema;
            return lexemas;
        }

        public void SetTas()  // carga las tas
        { tas.CargarTas(); }

        public void SetLexemas(string[] lexemastipos) //carga los lexemas para el analisis sintactico
        { lexemas = lexemastipos; }

        public bool EsTerminal(string token, string[] terminal) // se fija si el lexema es terminal
        {
            bool Resultado = false;

            for(int c = 0; c < terminal.Length; c++)
            {
                if(token==terminal[c])
                { Resultado = true; }
            }
            return Resultado;
        }


        public CArbol Arbol { get => this.arbol; }
    }
}
