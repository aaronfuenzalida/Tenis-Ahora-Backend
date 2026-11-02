"""
TC_005 - Validar la entrada de datos de cobro de seña y la salida/emisión del recibo digital en PDF.

Módulo: Gestión de Cobros y Recibos. Prioridad: High.
Precondición: reserva en estado "Pendiente de pago" (en el documento R-105, cliente DNI 45.225.625,
total $10.000) y usuario autenticado.

El PDF lo arma el front (Tenis-Ahora-Frontend/src/utils/receiptPdf.js) con el recibo que devuelve
GET /recibos/{id}. En el documento el paso 4 falló porque el PDF salió en blanco, sin número de
recibo ni datos del cobro; acá se verifica que el recibo traiga todos los datos que el PDF imprime.

Uso:  python tests/tc_005_cobro_sena_recibo.py [http://localhost:5090/api]
"""
import re
import sys

from casos_tp import ejecutar
from smoke import call, check, in_days, pay

ID = "TC_005"
TITULO = "Cobro de la seña con QR de Mercado Pago y recibo digital"

CANCHA = "cancha-3"
CLIENTE = {"name": "Sofía Benítez", "dni": "45.225.625", "phone": "11 4000-3001", "isLead": True}
JUGADORES = [CLIENTE, {"name": "Carla Méndez", "dni": "40.555.002", "phone": "11 4000-3002"}]


def run():
    call("POST", "/admin/reset", expect=200)
    call("PUT", f"/canchas/{CANCHA}", {"pricePerHour": 10000}, expect=200)
    st, res = call("POST", "/reservas", {"courtId": CANCHA, "date": in_days(1), "startTime": "10:00", "durationHours": 1,
                                         "playersType": "singles", "participants": JUGADORES}, expect=201)
    rid = res["data"]["id"]
    check("TC_005 precondición - reserva registrada en estado \"Pendiente de pago\"",
          res["data"]["status"] == "pendiente_pago", rid)

    st, r = call("GET", f"/reservas/{rid}", expect=200)
    check("TC_005 paso 1 - la reserva del cliente DNI 45.225.625 muestra seña de $5.000 (50 % de $10.000) y el saldo",
          r["participants"][0]["dni"] == CLIENTE["dni"] and r["totalCost"] == 10000
          and r["depositAmount"] == 5000 and r["remainingBalance"] == 5000)

    st, tx = pay(5000, "Mercado Pago (QR)")
    check("TC_005 paso 2 - el pago con QR de Mercado Pago por $5.000 vuelve aprobado",
          tx["paymentMethod"] == "Mercado Pago (QR)" and tx["amount"] == 5000 and tx["status"] == "approved"
          and re.fullmatch(r"MP-\d{8}", tx["id"]), tx["id"])

    st, conf = call("POST", f"/reservas/{rid}/pagar-sena", {"transactionId": tx["id"]}, expect=200)
    data = conf["data"]
    check("TC_005 paso 3 - recibe la confirmación, la reserva pasa a \"Confirmada\" y registra el cobro",
          data["status"] == "confirmada" and data["depositPaid"] == 5000
          and data["depositTransactionId"] == tx["id"] and data["depositPaymentMethod"] == "Mercado Pago (QR)")

    st, rec = call("GET", f"/recibos/{conf['receipt']['id']}", expect=200)
    campos = ("id", "clientName", "clientDni", "date", "paymentMethod", "transactionId", "reservationId", "concept")
    vacios = [k for k in campos if not rec.get(k)]
    items = rec.get("items") or []
    check("TC_005 paso 4 - el recibo trae número, datos de la reserva y desglose del cobro para el PDF",
          not vacios and rec["clientDni"] == CLIENTE["dni"] and rec["reservationId"] == rid
          and rec["transactionId"] == tx["id"] and rec["totalPaid"] == 5000
          and items and all(i.get("description") and "amount" in i for i in items)
          and any(i["amount"] == 5000 for i in items),
          f"campos vacíos: {vacios}" if vacios else rec["id"])

    st, final = call("GET", f"/reservas/{rid}", expect=200)
    check("TC_005 poscondición - la reserva queda confirmada con el número de recibo asociado",
          final["status"] == "confirmada" and final["depositReceiptNumber"] == rec["id"])


if __name__ == "__main__":
    sys.exit(ejecutar([sys.modules[__name__]]))
