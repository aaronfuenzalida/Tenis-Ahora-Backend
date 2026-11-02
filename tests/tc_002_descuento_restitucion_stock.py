"""
TC_002 - Validar el descuento y la restitución del stock de insumos deportivos al iniciar y
finalizar un alquiler.

Módulo: Gestión de Stock. Prioridad: High.
Precondición: 10 raquetas, 10 tubos de pelotas y 5 redes; reserva confirmada con la seña del 50 %.
El alquiler es de singles (2 jugadores): lleva 2 raquetas, 1 tubo de pelotas y 1 red.

Uso:  python tests/tc_002_descuento_restitucion_stock.py [http://localhost:5090/api]
"""
import sys

from casos_tp import ejecutar
from smoke import VISA_DEBITO, call, check, in_days, pay, stock_of

ID = "TC_002"
TITULO = "Descuento y restitución del stock al iniciar y finalizar un alquiler"

CANCHA = "cancha-2"  # C-02
JUGADORES = [{"name": "Tomás Ibarra", "dni": "40.222.001", "phone": "11 4000-1001", "isLead": True},
             {"name": "Diego Coria", "dni": "40.222.002", "phone": "11 4000-1002"}]
INICIAL = {"Raquetas": 10, "Pelotas": 10, "Redes": 5}


def niveles():
    return {c: stock_of(c)["availableStock"] for c in INICIAL}


def run():
    call("POST", "/admin/reset", expect=200)
    for category, total in INICIAL.items():
        call("PATCH", f"/stock/{stock_of(category)['id']}/ajuste", {"totalStock": total}, expect=200)
    st, res = call("POST", "/reservas", {"courtId": CANCHA, "date": in_days(1), "startTime": "10:00", "durationHours": 2,
                                         "playersType": "singles", "participants": JUGADORES}, expect=201)
    rid = res["data"]["id"]
    st, tx = pay(res["data"]["depositAmount"])
    st, conf = call("POST", f"/reservas/{rid}/pagar-sena", {"transactionId": tx["id"]}, expect=200)
    check("TC_002 precondición - stock 10/10/5 y reserva confirmada con la seña del 50 %",
          niveles() == INICIAL and conf["data"]["status"] == "confirmada"
          and conf["data"]["depositPaid"] == conf["data"]["totalCost"] / 2, rid)

    st, playing = call("POST", f"/reservas/{rid}/iniciar", expect=200)
    taken = {t["category"]: t["quantity"] for t in playing["equipmentTaken"]}
    check("TC_002 paso 1 - el alquiler pasa a \"En Juego\" con 2 raquetas, 1 tubo de pelotas y 1 red",
          playing["status"] == "en_juego" and taken == {"Raquetas": 2, "Pelotas": 1, "Redes": 1}, str(taken))

    durante = niveles()
    check("TC_002 paso 2 - descuento automático: 8 raquetas, 9 tubos de pelotas y 4 redes",
          durante == {"Raquetas": 8, "Pelotas": 9, "Redes": 4}, str(durante))

    saldo = playing["remainingBalance"]
    st, card = pay(saldo, "Tarjeta de Débito", VISA_DEBITO)
    st, fin = call("POST", f"/reservas/{rid}/pagar-saldo", {"transactionId": card["id"]}, expect=200)
    receipt = fin["receipt"]
    check("TC_002 paso 3 - cobra el 50 % restante con débito, emite el recibo y termina el alquiler",
          fin["data"]["status"] == "finalizada" and fin["data"]["durationHours"] == 2
          and saldo == fin["data"]["totalCost"] / 2 and receipt["totalPaid"] == saldo
          and receipt["paymentMethod"] == "Tarjeta de Débito" and receipt["cardLast4"] == "5619", receipt["id"])

    despues = niveles()
    check("TC_002 paso 4 - el stock vuelve a 10 raquetas, 10 tubos de pelotas y 5 redes",
          despues == INICIAL, str(despues))

    st, rec = call("GET", f"/recibos/{receipt['id']}")
    check("TC_002 poscondición - el cobro queda cerrado con el recibo guardado",
          st == 200 and rec["reservationId"] == rid and rec["transactionId"] == card["id"])


if __name__ == "__main__":
    sys.exit(ejecutar([sys.modules[__name__]]))
