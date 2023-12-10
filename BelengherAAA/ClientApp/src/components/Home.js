
import React, { useEffect, useRef, useState } from 'react';
import "mapbox-gl/dist/mapbox-gl.css";
import mapboxgl from 'mapbox-gl';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Paper from '@mui/material/Paper';
import * as PropTypes from "prop-types";
import {Card} from "reactstrap";
import {Button, CardContent, Typography} from "@mui/material";
import {Link} from "react-router-dom";
mapboxgl.accessToken = 'pk.eyJ1IjoibWloYWkwMjAwIiwiYSI6ImNsZzhjOTd2YjB3OTEzZ3A4bjl3bnA5bmsifQ.gq21MjyBrYrq8Skwya6HOA';


function CompanyCard(props) {
    if(props == null)
        return <></>;
    return (
        <Card>
            <CardContent>
                {props.name && (
                    <Typography variant="h5" component="div">
                        {props.name}
                    </Typography>
                )}
                {(props.company_name || props.main_country) && (
                    <Typography variant="subtitle1" color="textSecondary">
                        {props.company_name} - {props.main_country}
                    </Typography>
                )}
                {props.short_description && (
                    <Typography variant="body1" paragraph>
                        {props.short_description}
                    </Typography>
                )}
                {props.main_business_category && (
                    <Typography variant="subtitle2" color="textSecondary">
                        Business Category: {props.main_business_category}
                    </Typography>
                )}
                {props.company_supplier_types && props.company_supplier_types.length > 0 && (
                    <Typography variant="subtitle2" color="textSecondary">
                        Supplier Types: {props.company_supplier_types.join(', ')}
                    </Typography>
                )}
                {props.lat && props.lng  && (
                    <Typography variant="body2" color="textSecondary" paragraph>
                        Location: {props.lat}, {props.lng}
                    </Typography>
                )}

                {props.website_url && (
                    <Button variant="outlined" color="primary" href={props.website_url} target="_blank">
                        Visit Website
                    </Button>
                )}

                {props.primary_email && (
                    <Typography variant="body2" color="textSecondary" paragraph>
                        Contact: {props.primary_email}
                    </Typography>
                )}

                <div>
                    {props.facebook_url && (
                        <Link href={props.facebook_url} target="_blank" color="inherit" underline="hover">
                            Facebook {" "}
                        </Link>
                    )}
                    {props.twitter_url && (
                        <Link href={props.twitter_url} target="_blank" color="inherit" underline="hover">
                            Twitter{" "}
                        </Link>
                    )}
                    {props.instagram_url && (
                        <Link href={props.instagram_url} target="_blank" color="inherit" underline="hover">
                            Instagram{" "}
                        </Link>
                    )}
                    {props.linkedin_url && (
                        <Link href={props.linkedin_url} target="_blank" color="inherit" underline="hover">
                            LinkedIn{" "}
                        </Link>
                    )}
                    {props.youtube_url && (
                        <Link href={props.youtube_url} target="_blank" color="inherit" underline="hover">
                            YouTube{" "}
                        </Link>
                    )}
                </div>
            </CardContent>
        </Card>
    );
}

CompanyCard.propTypes = {data: PropTypes.any};

function CompanyInfo(props) {
    return null;
}

CompanyInfo.propTypes = {companyName: PropTypes.any};

function Home() {
    const inputElement = React.useRef()
    const mapContainer = useRef(null);
    const map = useRef(null);
    const [lng, setLng] = useState(26.1275);
    const [lat, setLat] = useState(44.4398);
    const [zoom, setZoom] = useState(11);
    const [companies, setCompanies] = useState([]);
    const [markerLng, setMarkerLng] = useState(0);
    const [marketLat,setMarkerLat] = useState(0);
    const [clicked,hasClicked]=useState(false);
    const [activity, setActivity] = useState("")
    const [currencyMarkersTrue, setCurrencyMarkers]=useState([]);
    const [rows, setRows] =useState([]);
    const [data,setData]=useState();
    const currentMarkers = [];
    function createData(name, description, ) {
        return { name, description};
    }

    let marker = null;
    function add_marker(e) {
        setMarkerLng(e.lngLat.lng);
        setMarkerLat(e.lngLat.lat);
        if (marker != null) {
            marker.setLngLat(e.lngLat);
            return;
        }
        //add marker
        marker = new mapboxgl.Marker({ "color": "#b40219" })
            .setLngLat(e.lngLat)
            .addTo(map.current)

    }
    useEffect(() => {
        async function fetchCompanies()
        { if(activity!=="") {
            let res = await fetch('https://localhost:7294/Geographic/GetNumberOfCompaniesWithSameActivityLevelInRangeAsync?latitude=' + lat + '&longitude=' + lng + '&activityLevels=' + activity);
            const companiesModel = await res.json();
            setCompanies(companiesModel);
            res=await fetch('https://localhost:7294/Geographic/AskGPT?latitude=' + lat + '&longitude=' + lng);
            const gptAnswer = await res.json();
            const createData = (name, value, description) => {
                return {
                    value: value,
                    description: description,
                    name: name
                };
            };

            const criminality_rate = createData("Crime rate", gptAnswer[0]["criminality_rate"]["field_value"], gptAnswer[0]["criminality_rate"]["field_explanation"]);
            const average_rental_price = createData("Average rental price", gptAnswer[1]["average_rental_price"]["field_value"],gptAnswer[1]["average_rental_price"]["field_explanation"]);
            const population_density = createData("Population denisty", gptAnswer[2]["population_density"]["field_value"],gptAnswer[2]["population_density"]["field_explanation"]);
            const public_transportation = createData("Public transportation", gptAnswer[3]["public_transportation_accessibility"]["field_value"],gptAnswer[3]["public_transportation_accessibility"]["field_explanation"]);
            const pollution_rate = createData("Pollution rate", gptAnswer[4]["pollution_rate"]["field_value"],gptAnswer[4]["pollution_rate"]["field_explanation"]);
            const dataList = [criminality_rate, average_rental_price, population_density, public_transportation, pollution_rate];

            setRows(dataList);
            hasClicked(false);
        }
        }

        fetchCompanies().then(r => r);
    }, [activity]);
    useEffect(() => {
        if (map.current) {
            if (currencyMarkersTrue!==null) {
                for (var i = currencyMarkersTrue.length - 1; i >= 0; i--) {
                    currencyMarkersTrue[i].remove();
                }
            }
            // eslint-disable-next-line array-callback-return
            companies.map(offer => {
                if (offer.longitude > -90 && offer.longitude < 90) {
                    const marker = new mapboxgl.Marker()
                        .setLngLat([offer.longitude,offer.latitude])
                        .addTo(map.current);
                    marker.getElement().addEventListener('click', () => {
                        setData(offer);
                        document.getElementById('exampleModalLong').ariaHidden=false;
                    });
                    currentMarkers.push(marker);
                    setCurrencyMarkers(currentMarkers);
                }
            })
            return;
        }
        // initialize map only once
        map.current = new mapboxgl.Map({
            container: mapContainer.current,
            style: 'mapbox://styles/mapbox/streets-v12',
            center: [lng, lat],
            zoom: zoom
        });
        marker = new mapboxgl.Marker({ "color": "#b40219" })
            .setLngLat({lng, lat})
            .addTo(map.current)
        map.current.addControl(
            new mapboxgl.GeolocateControl({
                positionOptions: {
                    enableHighAccuracy: true
                },
                trackUserLocation: true,
                showUserHeading: true
            })
        );
        map.current.on('click', add_marker);

    }, [companies]);
    useEffect(() => {
        if (!map.current) return; // wait for map to initialize
        map.current.on('move', () => {
            setLng(map.current.getCenter().lng.toFixed(4));
            setLat(map.current.getCenter().lat.toFixed(4));
            setZoom(map.current.getZoom().toFixed(2));
        });
    });

    function handleSubmit(e) {
        e.preventDefault()
        setActivity(e.target.elements.activity.value)
    }

    return (
        <div>
            <button hidden type="button" className="btn btn-primary" data-toggle="modal" data-target="#exampleModalLong">
                Launch demo modal
            </button>

            <div className="container">
                <div className="row">
                    <div className={"col"}></div>
                    <div className={"col"}></div>
                    <div className="col">
                        <div>
                            <form onSubmit={handleSubmit}>
                                <label>
                                    Name:
                                </label>
                                <input name="activity"/>
                                <button>Submit</button>
                            </form>
                        </div>
                    </div>
                    <div className="col">
                        <div>
                            <div className="sidebar">
                                Longitude: {lng} | Latitude: {lat} | Zoom: {zoom}
                            </div>
                            <div ref={mapContainer} className="map-container"/>
                        </div>
                    </div>
                    <div className="w-100"></div>
                    <div className="col"></div>
                    <div className="col" style={{display: 'flex', justifyContent: 'flex-end', flexWrap: 'wrap'}}>
                        <TableContainer component={Paper}>
                            <Table sx={{maxWidth: 100}} aria-label="simple table">
                                <TableHead>
                                    <TableRow>
                                        <TableCell align="left">Criterion</TableCell>
                                        <TableCell>Value</TableCell>
                                        <TableCell align="right">Description</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {rows.map((row) => (
                                        <TableRow
                                            key={row.name}
                                            sx={{'&:last-child td, &:last-child th': {border: 0}}}
                                        >
                                            <TableCell component="th" scope="row">
                                                {row.name}
                                            </TableCell>
                                            <TableCell align="right">{row.value}</TableCell>
                                            <TableCell align="right">{row.description}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    </div>
                </div>
            </div>
            <div className="modal fade" id="exampleModalLong" tabIndex="-1" role="dialog"
                 aria-labelledby="exampleModalLongTitle" aria-hidden="true">
                <div className="modal-dialog" role="document">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h5 className="modal-title" id="exampleModalLongTitle">Modal title</h5>
                            <button type="button" className="close" data-dismiss="modal" aria-label="Close">
                                <span aria-hidden="true">&times;</span>
                            </button>
                        </div>
                        <div className="modal-body">
                            <CompanyCard data={data}/>
                            <div></div>
                            <CompanyInfo companyName={data}/>
                        </div>
                        <div className="modal-footer">
                            <button type="button" className="btn btn-secondary" data-dismiss="modal">Close</button>
                            <button type="button" className="btn btn-primary">Save changes</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default Home;